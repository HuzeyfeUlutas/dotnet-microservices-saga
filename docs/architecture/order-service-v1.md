# Order Service V1 Plan

This document defines the initial scope and contract decisions for the `Order` service.

The goal of `Order` v1 is to implement a production-shaped checkout orchestration flow that stays aligned with the current repository templates and service boundaries.

## Migration Note

The first implementation used direct internal HTTP clients and persisted MassTransit consumers.

The approved migration direction is:

```text
immediate internal checkout calls -> direct gRPC
saga continuation and compensation -> MassTransit over RabbitMQ
checkout coordination -> persisted MassTransit state machine saga
```

Follow `docs/architecture/service-communication.md` for the repository-wide protocol rules.

## Core Responsibility

`Order` owns:

- checkout request orchestration
- order record creation
- order line purchase snapshot persistence
- payment-result driven order state progression
- orchestration of stock commit or release after payment result
- order-facing saga coordination

`Order` does not own:

- product catalog management
- stock truth or stock reservation rules
- payment provider lifecycle
- shipment
- notification delivery
- invoice creation
- basket lifecycle
- refund execution

## V1 Scope

`Order` v1 includes:

- `POST /api/checkout/pay`
- `GET /api/orders/{orderId}`
- local order persistence
- checkout idempotency
- payment initiation through `Payment`
- stock reservation through `Inventory`
- payment result consumption through saga orchestration
- stock commit/release orchestration through messaging

`Order` v1 excludes:

- order listing and search
- customer-initiated cancel
- refund orchestration
- shipment start
- notification publishing
- basket cleanup
- tax, shipping fee, coupon, and invoice modeling
- multi-currency beyond the snapshot returned by `Catalog`

## Why This Scope

This scope is intentionally narrow.

Reasons:

- the repository already has `Catalog`, `Inventory`, and `Payment` readiness decisions that define the checkout boundary
- `Order` should first become the checkout orchestrator before taking on post-order and customer-service workflows
- refund, cancel, shipment, and notification flows add additional states and compensation branches that are not required to make checkout operational
- the first usable vertical slice is: create order -> reserve stock -> initiate payment -> continue by saga -> finalize order state

## User-Facing Flow

The expected first checkout experience is:

1. frontend sends checkout request to `Order`
2. `Order` validates the request
3. `Order` fetches purchase snapshot data from `Catalog`
4. `Order` reserves stock in `Inventory`
5. `Order` persists a new order in `WaitingForPayment`
6. `Order` initiates payment in `Payment`
7. `Order` returns `orderId`, `orderStatus`, and `payment.action` to the frontend
8. frontend follows the provider action such as redirect or 3DS step
9. provider callback reaches `Payment`
10. `Payment` publishes result events
11. `Order` saga consumes those events and continues the distributed flow

The first synchronous checkout response must not imply that the order is confirmed.

The correct initial meaning is:

- order record exists
- payment flow has started
- stock is on temporary hold
- final success still depends on payment result and follow-up orchestration

## Service Boundaries

### Catalog

`Order` must fetch purchase snapshot data through the Catalog-owned internal gRPC method:

```text
GetPurchaseInfo(ProductId, Sku)
```

`Order` must persist its own line snapshot and must not read `Catalog` tables directly.

### Inventory

`Order` must reserve all checkout stock items synchronously through the Inventory-owned internal gRPC method:

```text
ReserveOrderStock(OrderId, Items[], ExpiresAtUtc)
```

Inventory must reserve all requested items atomically in one transaction.

Commit, release, and committed-stock reverse are asynchronous follow-up actions initiated by the `Order` saga through messaging:

```text
CommitStockRequested
ReleaseStockRequested
ReverseCommittedStockRequested
```

### Payment

`Order` initiates payment synchronously through the Payment-owned internal gRPC method and receives a provider-neutral `payment.action`:

```text
CreatePayment(OrderId, Amount, Currency, IdempotencyKey, Provider, Method)
```

Payment result handling remains owned by `Payment`.

`Order` does not handle provider callbacks directly.

`Order` reacts to payment result integration events such as:

- `PaymentAuthorized`
- `PaymentAuthorizationFailed`
- `PaymentCaptured`
- `PaymentCaptureFailed`

## Checkout Request Contract

`Order` v1 will accept direct line-item input instead of basket-based checkout.

Reason:

- there is no documented basket boundary yet
- current repository readiness work is based on product snapshot + stock reservation by product and SKU
- direct line input keeps the first integration path explicit and testable

Proposed request shape:

```json
{
  "buyerId": "00000000-0000-0000-0000-000000000000",
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000000",
      "sku": "SKU-001",
      "quantity": 1
    }
  ],
  "idempotencyKey": "checkout-unique-key",
  "payment": {
    "provider": "Fake",
    "method": "Card"
  }
}
```

### Included Fields

- `buyerId`
- `items[]`
- `idempotencyKey`
- payment provider/method preference

### Excluded Fields In V1

- shipping address
- billing address
- shipping option
- tax breakdown
- discount or coupon
- gift note
- basket id

Reasons:

- these concerns are not required to make the checkout orchestration flow work
- they would force premature expansion of the aggregate and response models
- there is no current repository contract that defines how those values should be owned or validated across services

## Checkout Response Contract

`Order` v1 will return:

```json
{
  "orderId": "00000000-0000-0000-0000-000000000000",
  "orderStatus": "WaitingForPayment",
  "payment": {
    "paymentId": "00000000-0000-0000-0000-000000000000",
    "status": "RequiresAction",
    "provider": "Fake",
    "action": {
      "type": "Redirect",
      "redirectUrl": "/fake-3ds/payments/payment-456"
    }
  }
}
```

Reason:

- this matches the existing `Payment` service boundary
- frontend needs a stable `orderId` immediately
- frontend also needs the provider-neutral action without understanding internal provider mechanics

## Order Read Contract

`GET /api/orders/{orderId}` is included in v1.

Order listing is not included in v1.

Reason:

- after redirect and asynchronous completion, the frontend needs a stable way to query final order status
- individual order lookup is enough for the first payment-driven flow
- search, filtering, pagination, and customer order history are separate read concerns and should be deferred

Proposed read shape:

```json
{
  "orderId": "00000000-0000-0000-0000-000000000000",
  "buyerId": "00000000-0000-0000-0000-000000000000",
  "status": "Confirmed",
  "currency": "TRY",
  "totalAmount": 199.90,
  "createdAtUtc": "2026-05-18T10:00:00Z",
  "updatedAtUtc": "2026-05-18T10:02:00Z",
  "failureReason": null,
  "items": [
    {
      "orderLineId": "00000000-0000-0000-0000-000000000000",
      "productId": "00000000-0000-0000-0000-000000000000",
      "sku": "SKU-001",
      "productName": "Sample Product",
      "variantName": "Default",
      "unitPrice": 199.90,
      "currency": "TRY",
      "quantity": 1,
      "lineTotal": 199.90
    }
  ]
}
```

## Snapshot Policy

Each order line must persist a purchase snapshot using the `Catalog` response and checkout quantity.

Required line snapshot fields:

- `ProductId`
- `Sku`
- `ProductName`
- `VariantName`
- `UnitPrice`
- `Currency`
- `Quantity`

Additional v1 order-level fields:

- `BuyerId`
- `TotalAmount`
- `FailureReason`

Not included in v1:

- seller snapshot
- brand snapshot
- category snapshot
- address snapshot
- tax snapshot
- shipping snapshot

Reason:

- only persist values that are already needed by checkout, payment initiation, and status display
- avoid snapshotting extra data before there is a defined owner and use case for it

## Order Status Model

`Order` v1 uses the following business states:

- `WaitingForPayment`
- `Confirmed`
- `PaymentFailed`
- `Failed`

`Cancelled` is intentionally not part of the v1 behavior.

Payment timeout cancellation is an internal saga compensation operation. It does not introduce a customer-facing cancellation API.

State guidance:

- `WaitingForPayment`: order exists, payment flow started, stock is reserved
- `Confirmed`: payment and stock commit succeeded
- `PaymentFailed`: payment authorization failed and stock release path completed
- `Failed`: unexpected orchestration or compensation failure requiring investigation

The state machine migration adds:

- `ManualReviewRequired`: one or more compensation steps could not be resolved automatically
- scheduled creation of `PaymentTimeoutExpired`

## Saga Boundary

The checkout saga belongs to `Order`.

Target implementation:

- persisted `MassTransitStateMachine<OrderCheckoutSagaState>` in `Order.Infrastructure`
- saga state persistence in `Order.Persistence`
- domain state transitions in `Order.Domain`
- Entity Framework saga repository backed by the Order database
- scheduled payment timeout after 15 minutes

The saga coordinates the flow but does not take ownership away from `Payment` or `Inventory`.

Migration note:

- `OrderCheckoutSagaState` implements `SagaStateMachineInstance`
- the state machine type and existing result-event correlations are defined in `Order.Infrastructure`
- the consumer-based bridge remains active until state-machine behaviors are migrated and the saga endpoint can replace it without duplicate orchestration

Responsibilities:

- react to payment result events
- request stock commit, release, or committed-stock reverse
- request payment capture, authorization void, or pending-payment cancellation
- wait for explicit result events from owning services
- move unresolved compensation failures to `ManualReviewRequired`
- update final order state

## Idempotency and Concurrency

### Checkout Idempotency

Checkout must be idempotent by client-provided `IdempotencyKey`.

Behavior:

1. if the same key is retried and the same order was already created, return the current order and payment initiation result shape when possible
2. do not create duplicate orders for the same idempotency key
3. do not re-run stock reservation or payment initiation blindly on retries

### Aggregate Concurrency

`Order` should use database-backed optimistic concurrency such as `RowVersion`.

Reason:

- saga and synchronous command flows may touch the same order
- retries, redelivery, and callback-driven progression can overlap
- conflicts should be explicit instead of silently overwriting state

## Messaging Direction

`Order` v1 will both consume and publish integration events.

Consume:

- payment result events from `Payment`
- inventory result events from `Inventory`

Publish:

- stock follow-up commands such as commit, release, and reverse requests
- payment follow-up commands such as capture, authorization void, and pending cancellation requests
- final order result events for downstream consumers

Why this decision:

- if `Order` saga owns orchestration, it must be able to drive follow-up async work
- adding transport-agnostic contracts now keeps the service aligned with repository messaging rules
- publish capability is required even if the first consumers are limited

## Reservation TTL

`Order` sends a short-lived reservation expiration time to `Inventory` and schedules a matching saga timeout.

Default direction:

- configuration-driven
- 15 minutes

Reason:

- long enough for fake 3DS or redirect-based payment flow
- short enough to avoid holding stock indefinitely
- infrastructure-configurable without reshaping the domain model

## Deferred Decisions

These are intentionally postponed until after checkout v1 is working:

- refund orchestration
- explicit cancel flow
- shipment initiation
- notification triggers
- order history/list endpoints
- taxes, fees, coupons
- address modeling
- customer-initiated refund orchestration beyond checkout compensation
