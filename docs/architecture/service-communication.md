# Service Communication Standard

This document defines the repository-wide communication rules between clients, API services, and microservices.

The goal is to keep synchronous calls explicit, asynchronous workflows reliable, and service boundaries independently deployable.

## Scope

The current implementation scope does not include an API Gateway.

When a gateway is added later, it will be responsible for client-facing north-south HTTP traffic. It must not become the default routing path for internal east-west service communication.

## Communication Matrix

Use the following default communication model:

| Caller | Callee | Purpose | Protocol |
| --- | --- | --- | --- |
| Frontend or external client | API service | Client-facing request/response | HTTP |
| External provider | Payment API | Callback or webhook delivery | HTTP |
| Microservice | Microservice | Immediate internal query or command that must return a result before the current request can continue | Direct gRPC |
| Microservice | Microservice | Saga continuation, compensation, integration fact, or background workflow | MassTransit over RabbitMQ |

Do not route internal gRPC calls through an API Gateway.

## Protocol Selection Rules

### Use HTTP For External Boundaries

Keep HTTP endpoints when the caller is outside the bounded context and the endpoint is intended for:

- frontend usage
- external client usage
- payment provider callback or webhook delivery
- operational endpoints such as health checks and metrics

Do not keep duplicate internal HTTP endpoints after their service-to-service callers have moved to gRPC or messaging unless there is a documented operational reason.

### Use gRPC For Immediate Internal Results

Use direct gRPC when the caller cannot continue its current request without the callee's response.

Current checkout examples:

```text
Order -> Catalog: fetch purchase snapshot
Order -> Inventory: reserve all checkout stock items atomically
Order -> Payment: create payment and receive provider-neutral payment action
```

gRPC services must:

- stay owned by the service that provides the capability
- call Application commands or queries instead of duplicating business logic
- use explicit deadlines
- propagate correlation metadata and tracing context
- return stable error semantics that callers map to Application exceptions

The local compose runtime exposes client-facing HTTP endpoints on container port `8080`. Services that provide internal gRPC capabilities expose a separate clear-text HTTP/2 endpoint on container port `8081`. `Order` reaches these endpoints directly through compose DNS:

```text
http://catalog-api:8081
http://inventory-api:8081
http://payment-api:8081
```

Keep the HTTP and gRPC listeners separate in the local runtime. Clear-text HTTP/2 negotiation is explicit on the internal gRPC listener and does not change the existing client-facing HTTP listener.

Keep runtime gRPC packages and build-time proto code generation packages explicit. `Grpc.Tools` is a build dependency and must remain private to the project that compiles `.proto` files. The local Docker build pins `Grpc.Tools` to `2.68.1` because later codegen binaries currently regress on Linux ARM64 containers; runtime server and client packages remain independently pinned.

The checkout reservation boundary must be order-level and batch-oriented:

```text
ReserveOrderStock(OrderId, Items[], ExpiresAtUtc)
```

Inventory must reserve all requested items inside one database transaction. If one item fails, no checkout reservation from that request may remain persisted.

### Use RabbitMQ For Workflow Progression

Use MassTransit over RabbitMQ for operations that continue after the initial request, especially when retry, compensation, redelivery, or delayed execution matters.

Current checkout examples:

```text
CommitStockRequested
ReleaseStockRequested
ReverseCommittedStockRequested
CapturePaymentRequested
VoidPaymentAuthorizationRequested
CancelPendingPaymentRequested
```

Result messages must be explicit:

```text
StockCommitted
StockCommitFailed
StockReleased
StockReleaseFailed
CommittedStockReversed
CommittedStockReverseFailed
PaymentCaptured
PaymentCaptureFailed
PaymentAuthorizationVoided
PaymentAuthorizationVoidFailed
PaymentCancelled
PaymentCancellationFailed
```

Do not call another service synchronously from a saga continuation step when the result can be modeled as a command and a later result event.

## Saga Rule

Long-running checkout coordination belongs to the `Order` bounded context.

Use a persisted MassTransit state machine saga:

```text
MassTransitStateMachine<TSaga>
SagaStateMachineInstance
Entity Framework saga repository
```

The saga:

- correlates workflow messages
- records the current workflow state
- schedules payment timeout handling
- publishes continuation and compensation commands
- waits for explicit result events
- moves unresolved compensation failures to manual review

The saga must not:

- implement Catalog rules
- mutate Inventory tables
- execute payment provider operations
- duplicate Application or Domain business rules

Each owning service protects its own invariants and reports the outcome through a result event.

## Checkout Compensation Rule

Authorization, capture, refund, and void are different payment operations.

```text
Authorization: place a temporary payment hold
Capture: collect an authorized payment
Refund: return money after capture
VoidAuthorization: cancel an authorization hold before capture
```

When payment capture fails after stock was committed:

```text
PaymentCaptureFailed
-> ReverseCommittedStockRequested
-> CommittedStockReversed
-> VoidPaymentAuthorizationRequested
-> PaymentAuthorizationVoided
-> OrderFailed when both compensations succeed
-> ManualReviewRequired when any compensation remains unresolved
```

Do not issue a refund when no payment was captured.

The persisted MassTransit state machine endpoint owns the successful checkout path plus payment authorization failure, stock commit failure, payment capture failure, and scheduled payment timeout branches. The timeout branch schedules `PaymentTimeoutExpired` after 15 minutes and applies compensation steps sequentially through state-machine activities. The previous consumer-based orchestration bridge has been removed.

## Contract Ownership

Do not create a repository-wide shared integration-contract or gRPC-contract assembly by default.

Each service must remain independently deployable and keep the contracts it publishes or consumes inside its own boundary.

### Messaging Contracts

Producer and consumer services may keep local copies of the message schema. Those local copies must preserve the same wire identity and compatible fields.

For every cross-service message:

- define a stable message name
- define a stable namespace or explicit MassTransit entity name
- document required fields
- preserve existing fields when evolving the schema
- add compatibility tests between producer and consumer schema copies

Do not add direct project references from one service to another service's `Application` project.

### gRPC Contracts

The provider service owns the canonical `.proto` definition for the capability it exposes.

Consumer services may keep a local client-side copy to preserve independent service boundaries.

Copied proto contracts must preserve:

- package name
- service name
- method name
- field numbers
- field types
- backward-compatible evolution rules

Add compatibility tests to detect schema drift.

## Transaction and Reliability Rules

- Use the owning service database as the consistency boundary.
- Keep database mutations and outgoing messages atomic through the MassTransit EF Core outbox.
- Use consumer outbox when a consumer both changes database state and publishes follow-up messages.
- Make command handlers idempotent because message redelivery is expected.
- Use optimistic concurrency for aggregates and saga instances.
- Do not rely on in-memory locks for cross-instance correctness.
- Add retry and delayed redelivery only after the affected operation is idempotent.

## Correlation and Observability

The repository correlation identifier remains:

```text
X-Correlation-Id
```

Propagation rules:

- accept or generate it for incoming HTTP requests
- propagate it through gRPC metadata
- propagate it through MassTransit headers and correlation metadata
- include it in structured logs
- preserve OpenTelemetry tracing across HTTP, gRPC, and messaging boundaries

Operational endpoints remain HTTP:

```text
/health/live
/health/ready
/metrics
```

## Current Migration Direction

The Order checkout flow migration is tracked in this order:

1. Completed: `Order -> Catalog` purchase snapshot HTTP call to gRPC
2. Completed: `Order -> Inventory` line-by-line HTTP reservation calls to one atomic batch gRPC call
3. Completed: `Order -> Payment` create-payment HTTP call to gRPC
4. Completed: Inventory commit and release HTTP calls to MassTransit commands and result events
5. Completed: committed-stock reverse compensation through MassTransit commands and result events
6. Completed: Payment authorization void and pending cancellation through MassTransit commands and result events
7. Pending: Order consumer-based orchestration to a persisted MassTransit state machine saga
8. Completed: removal of internal-only HTTP reservation endpoints
