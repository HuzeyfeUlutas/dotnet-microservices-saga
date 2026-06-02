# Payment Checkout Flow

This document records the initial payment and checkout workflow decision for the marketplace platform.

The goal is to support a simulated payment provider now while keeping the design compatible with providers such as Stripe, Iyzico, PayTR, or another 3D Secure capable provider later.

## Core Decision

The platform will use a hybrid checkout flow:

1. The first checkout/payment request performs only the minimum synchronous work needed to return a payment action to the frontend.
2. The frontend immediately follows that payment action, for example redirecting to 3D Secure or calling a provider SDK.
3. Provider callback/webhook results are handled by the Payment service.
4. The Order saga continues the distributed workflow after the payment result is known.

This keeps the user experience close to a normal ecommerce checkout while still using saga orchestration for the distributed transaction after the external payment step.

Internal protocol rules follow:

```text
Order -> Catalog purchase snapshot: direct gRPC
Order -> Inventory atomic batch reservation: direct gRPC
Order -> Payment initiation: direct gRPC
Saga continuation and compensation: MassTransit over RabbitMQ
```

Do not route internal gRPC calls through an API Gateway.

## User-Facing Flow

```text
1. Frontend -> POST /checkout/pay
2. Order Service creates an order with status WaitingForPayment
3. Inventory stock is reserved
4. Payment Service creates a payment intent/session
5. Order Service returns PaymentAction to the frontend
6. Frontend follows the PaymentAction immediately
7. Provider callback/webhook reaches Payment Service
8. Payment Service publishes a payment result event
9. Order Saga continues from the payment result
10. Stock and payment operations are completed or compensated, and the order is confirmed/failed
```

## Synchronous Request Scope

The first checkout payment request may do:

```text
create order in WaitingForPayment state
reserve stock with a short-lived hold
create payment intent/session
return PaymentAction
```

The first checkout payment request must not do:

```text
confirm the order
send notifications
start shipment
create invoice
clean the basket as a final fact
run seller payout logic
wait for all downstream events to complete
```

Those follow-up tasks should happen asynchronously after payment result events.

## Catalog Data Boundary for Checkout

Order/checkout should not copy Catalog tables or read Catalog's database directly.

Catalog exposes the product data needed by Order through an internal gRPC method:

```text
GetPurchaseInfo(ProductId, Sku)
```

Order should use this read contract before creating an order line snapshot.

The order line snapshot should keep the values needed to preserve the purchase record even if Catalog changes later:

```text
ProductId
Sku
ProductName
VariantName
UnitPrice
Currency
Quantity
```

Catalog currently returns `Currency` as `TRY` because Catalog does not yet model currency per product. If multi-currency pricing is introduced, Catalog must own the product price currency and the purchase-info response should stop using a hardcoded default.

Checkout stock validation and reservation remain Inventory-owned. Catalog purchase-info only answers catalog purchasability, naming, and price snapshot data.

Catalog purchasability should be treated as a pre-check. Inventory remains the source of truth for stock.

## PaymentAction Contract

The frontend should not depend on one provider-specific shape such as only a redirect URL.

The backend should return a provider-neutral payment action.

Example for a fake or redirect-based provider:

```json
{
  "orderId": "order-123",
  "orderStatus": "WaitingForPayment",
  "payment": {
    "paymentId": "payment-456",
    "status": "RequiresAction",
    "provider": "Fake",
    "action": {
      "type": "Redirect",
      "redirectUrl": "/fake-3ds/payments/payment-456"
    }
  }
}
```

Example for Stripe-like providers:

```json
{
  "orderId": "order-123",
  "orderStatus": "WaitingForPayment",
  "payment": {
    "paymentId": "payment-456",
    "status": "RequiresAction",
    "provider": "Stripe",
    "action": {
      "type": "ClientSecret",
      "clientSecret": "pi_xxx_secret_yyy"
    }
  }
}
```

Example for HTML form based providers:

```json
{
  "orderId": "order-123",
  "orderStatus": "WaitingForPayment",
  "payment": {
    "paymentId": "payment-456",
    "status": "RequiresAction",
    "provider": "Iyzico",
    "action": {
      "type": "HtmlForm",
      "htmlContent": "<form method=\"post\" action=\"https://provider.example/3ds\">...</form>"
    }
  }
}
```

Suggested action types:

```text
None
Redirect
ClientSecret
HtmlForm
```

Provider API keys and backend secrets must never be returned to the frontend. Client secrets or tokens returned to the frontend must be values explicitly intended by the provider for frontend usage.

## Saga Responsibility

The Order saga does not directly return an HTTP response to the frontend.

The checkout endpoint returns the initial PaymentAction. The saga is responsible for continuing the order workflow after payment result events arrive.

Successful path:

```text
PaymentAuthorized
-> CommitStockRequested
-> StockCommitted
-> CapturePaymentRequested
-> PaymentCaptured
-> OrderConfirmed
```

Failed authorization path:

```text
PaymentAuthorizationFailed
-> ReleaseStockRequested
-> StockReleased
-> OrderPaymentFailed
```

Capture failure path:

```text
PaymentCaptureFailed
-> ReverseCommittedStockRequested
-> CommittedStockReversed
-> VoidPaymentAuthorizationRequested
-> PaymentAuthorizationVoided
-> OrderFailed
```

Stock commit failure path:

```text
StockCommitFailed
-> ReleaseStockRequested
-> StockReleased
-> VoidPaymentAuthorizationRequested
-> PaymentAuthorizationVoided
-> OrderFailed
```

Payment timeout path:

```text
PaymentTimeoutExpired
-> CancelPendingPaymentRequested
-> PaymentCancelled
-> ReleaseStockRequested
-> StockReleased
-> OrderPaymentFailed
```

If any compensation cannot be resolved automatically, the saga must move to `ManualReviewRequired`.

The persisted MassTransit state machine endpoint owns the successful checkout path plus payment authorization failure, stock commit failure, and payment capture failure branches. The consumer-based bridge currently applies only the timeout continuation steps sequentially. Scheduled creation of `PaymentTimeoutExpired` remains part of the state machine migration.

Do not silently confirm an order unless stock and payment invariants are satisfied.

## Payment Operation Semantics

Do not treat authorization void and refund as the same operation.

```text
Authorization: place a temporary hold
Capture: collect an authorized payment
VoidAuthorization: cancel a hold before capture
Refund: return money after capture
```

When capture fails, request authorization void. Do not request refund because no captured payment exists.

## Payment Service Responsibility

Payment Service owns:

```text
payment lifecycle
payment attempts
provider abstraction
fake provider simulation
provider callback/webhook handling
webhook signature verification
webhook idempotency
payment state transition guards
payment integration events
```

Payment Service does not own:

```text
order confirmation
stock reservation rules
basket cleanup
shipment
notification orchestration
checkout saga state
```

## Current Payment Boundary

The Payment service implementation must stay limited to payment lifecycle ownership.

Current Payment service responsibilities in code:

```text
create payment records
return provider-neutral PaymentAction values
simulate fake 3DS authorization
handle provider callback/webhook input
guard payment state transitions in the Domain model
persist payment lifecycle state in the Payment database
publish payment result integration events through the transactional outbox
```

Current Payment service non-responsibilities in code:

```text
do not reference Order projects
do not create or update orders
do not reserve, commit, or release stock
do not own checkout saga state
do not confirm or fail orders directly
do not call Order APIs from Payment handlers
```

The Order saga must consume Payment integration events and decide the next distributed workflow step.

Payment integration events currently produced by the Payment service:

```text
PaymentAuthorized
PaymentAuthorizationFailed
PaymentCaptured
PaymentCaptureFailed
PaymentRefunded
PaymentRefundFailed
```

Required additions for checkout compensation:

```text
VoidPaymentAuthorizationRequested
PaymentAuthorizationVoided
PaymentAuthorizationVoidFailed
CancelPendingPaymentRequested
PaymentCancelled
PaymentCancellationFailed
```

## Required Reliability Rules

The implementation should include:

```text
idempotency key for checkout/payment initiation
unique provider event processing for webhooks
optimistic concurrency for payment state transitions
provider signature verification for webhooks
transactional outbox for published payment events
short timeouts for provider calls
safe retry policies
explicit failed and manual review states
reservation expiration in Inventory
```

## Repository Rule Alignment

Initial Payment service scaffolding must follow:

```text
docs/templates/service-skeleton.md
```

Payment Domain common base types must follow:

```text
docs/templates/domain-common.md
```

Payment Domain exceptions must follow:

```text
docs/templates/domain-exceptions.md
```

Payment Application layer must follow:

```text
docs/templates/application-skeleton.md
```

Messaging and outbox behavior must follow:

```text
docs/architecture/messaging-masstransit.md
```
