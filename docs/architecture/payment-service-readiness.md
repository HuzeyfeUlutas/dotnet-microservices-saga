# Payment Service Readiness

This document records the current Payment service behavior and the architectural decisions applied before deeper Order saga integration.

The goal is to keep Payment limited to payment lifecycle ownership while making it reliable enough to participate in the checkout and order workflow.

## Service Responsibility

Payment owns:

```text
payment lifecycle state
payment authorization, capture, and refund transitions
payment attempt tracking
provider callback/webhook processing
provider callback idempotency
payment result integration event publication
inbound payment operation consumption for saga continuation
payment-specific observability and health signals
```

Payment does not own:

```text
order creation or confirmation
inventory reservation, commit, or release
checkout saga state
basket cleanup
shipment
notification orchestration
```

## Current Payment Model

The service currently persists:

```text
Payment
PaymentAttempt
ProcessedProviderCallback
```

### Payment

`Payment` is the aggregate root and owns:

```text
OrderId
Amount
Provider
Method
Status
IdempotencyKey
provider payment/transaction references
authorization/capture/refund timestamps
optimistic concurrency row version
```

### PaymentAttempt

`PaymentAttempt` records each authorization, capture, or refund attempt and keeps:

```text
attempt number
attempt type
provider
attempt status
attempt idempotency key
provider action/reference values
failure reason
```

### ProcessedProviderCallback

`ProcessedProviderCallback` stores processed webhook/provider callback identities:

```text
PaymentId
Provider
ProviderEventId
ProcessedAtUtc
```

Uniqueness boundary:

```text
Provider + ProviderEventId
```

This prevents duplicate provider callbacks from replaying payment state transitions.

## Idempotency Rules

### Payment Creation

Payment initiation is idempotent by:

```text
Payment.IdempotencyKey
```

Behavior:

1. if a payment with the same idempotency key exists, the existing payment is returned
2. if that existing payment is in `RequiresAction`, the existing provider-neutral action is returned
3. the provider authorization flow is not restarted for an already-created payment

### Provider Callback

Provider callback processing is idempotent by:

```text
ProcessedProviderCallback.Provider + ProcessedProviderCallback.ProviderEventId
```

Behavior:

1. if the same provider event id arrives again, the handler returns the current payment state
2. if the payment is already in a final or advanced state, the callback does not re-run authorization logic
3. callback processing and outgoing payment result event publication remain in the same persistence transaction boundary

## Concurrency Rules

Payment uses optimistic concurrency through:

```text
Payment.RowVersion
```

Purpose:

- protect payment state from overlapping updates
- avoid duplicate capture/refund effects during retries or redelivery
- keep conflicting updates explicit

Current behavior:

- duplicate capture requests return the current payment when status is already `Captured`
- duplicate refund requests return the current payment when status is already `Refunded`
- duplicate or racing authorization callback processing returns the current payment state when possible
- concurrency conflicts are surfaced as application conflicts when the operation cannot be resolved idempotently

## Inbound Messaging Boundary

Payment now supports inbound saga continuation messages through transport-agnostic contracts:

```text
CapturePaymentRequested
RefundPaymentRequested
```

Consumer behavior:

1. consume incoming integration event
2. log event metadata
3. dispatch the corresponding MediatR command
4. keep payment business rules in application handlers, not in MassTransit consumers

This keeps the Payment service aligned with the repository messaging standard and allows Order saga continuation without direct HTTP coupling.

## Outbound Messaging Boundary

Payment currently publishes these result events through the transactional outbox:

```text
PaymentAuthorized
PaymentAuthorizationFailed
PaymentCaptured
PaymentCaptureFailed
PaymentRefunded
PaymentRefundFailed
```

The current publishing flow is:

1. update payment aggregate state
2. publish transport-agnostic event through `IIntegrationEventPublisher`
3. persist payment state and outbox record atomically
4. allow MassTransit outbox delivery to forward the message to RabbitMQ

## Provider Boundary

The current provider implementation is still `FakePaymentProvider`.

Current fake-provider behavior:

```text
returns redirect-style payment action for authorization
simulates 3DS completion
simulates capture success
simulates refund success
```

The provider abstraction remains ready for future real integrations, but the service is still development-focused at the provider layer.

## Observability

Payment now exposes the repository-standard operational endpoints:

```text
GET /health/live
GET /health/ready
GET /metrics
```

Payment now also includes:

```text
Serilog structured request logging
X-Correlation-Id propagation
OpenTelemetry tracing
Prometheus metrics export
RabbitMQ and PostgreSQL readiness checks
```

## Test Coverage Baseline

Current Payment test coverage includes:

```text
domain authorization and capture guard rules
payment creation idempotency behavior
provider callback idempotency behavior
already-captured payment handling
```

This is the initial baseline, not the final test target.

Recommended next coverage additions:

```text
refund duplicate handling
consumer-to-command mapping tests
provider callback persistence uniqueness tests
ProblemDetails mapping tests
```

## Current Readiness Summary

Before Order saga integration, Payment is now ready in these areas:

```text
provider callback idempotency
transactional payment result publication
inbound capture/refund message boundary
basic optimistic concurrency handling
payment-specific health/metrics/correlation wiring
baseline domain and application tests
```

Known remaining limitations:

```text
provider signature verification is not implemented because only the fake provider exists
manual review states are not modeled yet
consumer-specific infrastructure tests are still missing
real provider timeout/retry policies are not yet implemented
```
