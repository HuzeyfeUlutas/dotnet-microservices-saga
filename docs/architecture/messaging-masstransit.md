# Messaging Standard

This repository standardizes on `MassTransit` as the application message bus and `RabbitMQ` as the broker for service-to-service messaging.

## Default Stack

- Transport: `RabbitMQ`
- Bus library: `MassTransit`
- Database consistency pattern: `Transactional Outbox`
- Persistence strategy for outbox: `Entity Framework Core` outbox tables in the owning service database

## Architecture Rules

- Do not publish broker messages directly from controllers.
- Do not couple the Application layer to `MassTransit`, `RabbitMQ.Client`, or broker-specific APIs.
- Define integration event contracts in a transport-agnostic location.
- Keep published and consumed contract models inside each service boundary.
- Do not add direct project references to another service's `Application` project.
- Do not introduce a repository-wide shared contracts assembly by default.
- Keep message publishing behind an Application abstraction and implement that abstraction in `{ServiceName}.Infrastructure`.
- Configure `MassTransit` in `{ServiceName}.Infrastructure`.
- Configure `DbContext` and outbox entities in `{ServiceName}.Persistence`.
- Use the owning service database for outbox storage. Do not create a shared messaging database.

## Service-Local Contract Rule

Producer and consumer services may keep separate local C# contract models for the same wire message.

Those models must preserve:

- a stable wire message name
- a stable namespace or explicit MassTransit entity name
- compatible required fields
- backward-compatible evolution

Add compatibility tests for producer and consumer schema copies. Do not assume two records with similar properties automatically represent the same MassTransit wire contract.

## Outbox Rule

Every service that publishes integration events must use the `MassTransit` bus outbox with its own `DbContext`.

Why:

- database state and outgoing messages are persisted atomically
- broker outages do not break the request transaction
- messages are delivered asynchronously after the database transaction succeeds

Do not use direct broker publish as the default write-path for commands that also mutate the database.

## Catalog Guidance

For `Catalog`, the current event publication flow is:

1. Application use case changes aggregate state.
2. Application use case publishes a transport-agnostic integration event through an abstraction.
3. `MassTransit` stores that outgoing message in the EF Core outbox.
4. `SaveChangesAsync` commits business data and outbox records in the same database transaction.
5. The outbox delivery service forwards pending messages to `RabbitMQ`.

### Catalog Product Events

`Catalog` currently publishes product integration events from the Application layer through `IIntegrationEventPublisher`.

Current product event contracts:

```text
ProductCreatedIntegrationEvent
ProductPriceUpdatedIntegrationEvent
ProductUnavailableIntegrationEvent
ProductVariantUnavailableIntegrationEvent
```

Contract rules:

- `ProductCreatedIntegrationEvent` identifies the product and its initial catalog references.
- `ProductPriceUpdatedIntegrationEvent` carries old and new product price values.
- `ProductUnavailableIntegrationEvent` is product-level and must include the affected variant/SKU snapshots so downstream consumers can invalidate availability/cache entries without querying Catalog again.
- `ProductVariantUnavailableIntegrationEvent` is variant-level and must include `ProductId`, `VariantId`, `Sku`, and a reason.
- Product and variant unavailable events are used by downstream services or cache updaters to mark SKUs unavailable for new basket/checkout attempts.

Do not remove SKU information from unavailable events unless another explicit lookup contract replaces it.

## Consumer Guidance

When consumers are added later:

- define consumers in the owning service infrastructure boundary
- configure endpoint names with a consistent formatter
- add retry/redelivery intentionally per endpoint
- use consumer outbox where handlers both consume and publish

Do not add broad retry policies without understanding idempotency and downstream effects.

## State Machine Saga Guidance

Use a persisted MassTransit state machine saga for a long-running workflow when:

- multiple services participate
- delayed execution or timeout matters
- compensation branches exist
- explicit workflow states improve diagnosis and recovery

The Order checkout workflow must use:

```text
MassTransitStateMachine<OrderCheckoutSagaState>
SagaStateMachineInstance
Entity Framework saga repository
```

Keep business rules in the owning service's Application and Domain layers. The saga coordinates commands and result events; it must not mutate another service's data or duplicate its business rules.

For RabbitMQ-backed delayed saga events, configure MassTransit's delayed message scheduler and run RabbitMQ with the delayed-exchange plug-in enabled. Keep scheduled-message token ids in the persisted saga state.

The local compose runtime uses the pinned `masstransit/rabbitmq:3.13.1` image because it includes the management interface and the delayed-exchange plug-in. Its RabbitMQ health check must verify both broker availability and `rabbitmq_delayed_message_exchange` activation.

The delayed-exchange plug-in is appropriate for the current 15-minute checkout timeout. Treat it as a bounded-delay mechanism, not as the default choice for long-term or very high-volume scheduling.

## Versioning Guidance

- Pin `MassTransit` package versions explicitly.
- Keep `MassTransit`, `MassTransit.RabbitMQ`, and `MassTransit.EntityFrameworkCore` on the same version.
- When EF Core major compatibility matters, choose a `MassTransit.EntityFrameworkCore` version compatible with the repository EF Core baseline before upgrading.
