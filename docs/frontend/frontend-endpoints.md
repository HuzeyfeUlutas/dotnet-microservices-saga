# Frontend Endpoint Guide

This document is the frontend-facing HTTP contract for the local demo.

Use only the API Gateway as the browser/client base URL. Do not call service ports directly from the frontend.

```text
Base URL: http://localhost:8085
```

## Authentication

Local identity provider:

```text
Keycloak issuer: http://localhost:8086/realms/marketplace
Frontend client: marketplace-frontend
Recommended frontend flow: Authorization Code with PKCE
API audience: marketplace-api
```

For protected endpoints send:

```http
Authorization: Bearer <access_token>
```

Do not send these headers from the frontend:

```text
X-User-Id
X-User-Email
X-User-Roles
```

The Gateway strips client-supplied identity headers and recreates them after JWT validation.

## Demo Users

All local demo users use password `Password123!`.

| Username | Roles | Intended use |
| --- | --- | --- |
| `customer1` | `customer` | checkout, order/payment view |
| `admin` | `admin`, `customer`, `catalog-manager`, `inventory-manager`, `support` | full demo access |
| `catalogmanager` | `catalog-manager` | catalog mutations |
| `inventorymanager` | `inventory-manager` | inventory management |
| `support` | `support` | notification support screens |

## Common Behavior

| Case | Expected status |
| --- | --- |
| Missing token on protected route | `401` |
| Valid token but missing role | `403` |
| Validation failure | `400` ProblemDetails |
| Resource not found | `404` ProblemDetails |
| Conflict/idempotency mismatch | `409` ProblemDetails |

Responses use JSON. Error responses include `traceId`; many services also include `correlationId`.

Enums are currently serialized as numbers unless explicitly returned as strings by a DTO. Important values:

| Enum | Values |
| --- | --- |
| ProductStatus | `1` Draft, `2` Active, `3` Inactive, `4` Archived |
| VariantStatus | `1` Active, `2` Inactive |
| OrderStatus | `1` WaitingForPayment, `2` Confirmed, `3` PaymentFailed, `4` Failed |
| PaymentStatus | `1` Pending, `2` RequiresAction, `3` Authorized, `4` AuthorizationFailed, `5` Captured, `6` CaptureFailed, `7` Refunded, `8` RefundFailed, `9` Cancelled, `10` AuthorizationVoided, `11` AuthorizationVoidFailed |
| NotificationChannel | `1` Email |
| NotificationMessageStatus | `1` Pending, `2` Processing, `3` Sent, `4` Failed, `5` Cancelled, `6` Skipped |

## Route Summary

| Endpoint | Auth | Roles |
| --- | --- | --- |
| `GET /api/products` | Anonymous | none |
| `GET /api/products/{id}` | Anonymous | none |
| `GET /api/categories` | Anonymous | none |
| `GET /api/categories/{id}` | Anonymous | none |
| `GET /api/brands` | Anonymous | none |
| `GET /api/brands/{id}` | Anonymous | none |
| Catalog mutations | Protected | `admin`, `catalog-manager` |
| Inventory endpoints | Protected | `admin`, `inventory-manager` |
| `POST /api/checkout/pay` | Protected | `admin`, `customer` |
| `GET /api/orders/{orderId}` | Protected | `admin`, `customer` |
| `GET /api/payments/{id}` | Protected | `admin`, `customer` |
| `/fake-3ds/payments/**` | Demo public | none |
| Notification preferences | Protected | any authenticated user |
| Notification support endpoints | Protected | `admin`, `support` |

## Catalog

Catalog GET endpoints are public. Mutation endpoints require `admin` or `catalog-manager`.

### Brands

#### GET /api/brands

Returns brand list items.

Response item shape:

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Nike",
  "isActive": true
}
```

#### GET /api/brands/{id}

Returns one brand.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Nike",
  "description": "Sports brand",
  "isActive": true
}
```

#### POST /api/brands

Roles: `admin`, `catalog-manager`

Request:

```json
{
  "name": "Nike",
  "description": "Sports brand"
}
```

Success: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

#### PUT /api/brands/{id}

Roles: `admin`, `catalog-manager`

```json
{
  "name": "Nike",
  "description": "Updated description",
  "isActive": true
}
```

Success: `204 No Content`

#### DELETE /api/brands/{id}

Roles: `admin`, `catalog-manager`

Success: `204 No Content`

### Categories

#### GET /api/categories

Returns category list items.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Shoes",
  "parentCategoryId": null,
  "isActive": true
}
```

#### GET /api/categories/{id}

Returns one category.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Shoes",
  "description": "Footwear",
  "parentCategoryId": null,
  "isActive": true
}
```

#### POST /api/categories

Roles: `admin`, `catalog-manager`

```json
{
  "name": "Shoes",
  "description": "Footwear",
  "parentCategoryId": null
}
```

Success: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

#### PUT /api/categories/{id}

Roles: `admin`, `catalog-manager`

```json
{
  "name": "Shoes",
  "description": "Updated description",
  "parentCategoryId": null,
  "isActive": true
}
```

Success: `204 No Content`

#### DELETE /api/categories/{id}

Roles: `admin`, `catalog-manager`

Success: `204 No Content`

### Products

#### GET /api/products

Returns product list items.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Running Shoe",
  "price": 199.90,
  "brandId": "00000000-0000-0000-0000-000000000000",
  "categoryId": "00000000-0000-0000-0000-000000000000",
  "status": 2
}
```

#### GET /api/products/{id}

Returns one product with variants and images.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "name": "Running Shoe",
  "description": "Demo product",
  "price": 199.90,
  "brandId": "00000000-0000-0000-0000-000000000000",
  "categoryId": "00000000-0000-0000-0000-000000000000",
  "status": 2,
  "variants": [
    {
      "id": "00000000-0000-0000-0000-000000000000",
      "name": "Default",
      "sku": "SKU-001",
      "status": 1
    }
  ],
  "images": []
}
```

#### POST /api/products

Roles: `admin`, `catalog-manager`

```json
{
  "name": "Running Shoe",
  "description": "Demo product",
  "price": 199.90,
  "brandId": "00000000-0000-0000-0000-000000000000",
  "categoryId": "00000000-0000-0000-0000-000000000000"
}
```

Success: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

#### PUT /api/products/{id}

Roles: `admin`, `catalog-manager`

```json
{
  "name": "Running Shoe",
  "description": "Updated product",
  "price": 199.90,
  "brandId": "00000000-0000-0000-0000-000000000000",
  "categoryId": "00000000-0000-0000-0000-000000000000",
  "status": 2
}
```

Success: `204 No Content`

#### DELETE /api/products/{id}

Roles: `admin`, `catalog-manager`

Success: `204 No Content`

#### POST /api/products/{id}/variants

Roles: `admin`, `catalog-manager`

```json
{
  "name": "Default",
  "sku": "SKU-001"
}
```

Success: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

#### PUT /api/products/{id}/variants/{variantId}

Roles: `admin`, `catalog-manager`

```json
{
  "name": "Default",
  "sku": "SKU-001"
}
```

Success: `204 No Content`

#### POST /api/products/{id}/variants/{variantId}/activate

Roles: `admin`, `catalog-manager`

Success: `204 No Content`

#### POST /api/products/{id}/variants/{variantId}/deactivate

Roles: `admin`, `catalog-manager`

Success: `204 No Content`

## Inventory

Inventory endpoints require `admin` or `inventory-manager`.

Use these endpoints for admin/demo stock screens only. Public product browsing should not call inventory in this demo.

#### GET /api/inventory-items

Returns inventory list items.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "productId": "00000000-0000-0000-0000-000000000000",
  "sku": "SKU-001",
  "totalQuantity": 10,
  "reservedQuantity": 2,
  "availableQuantity": 8,
  "isActive": true
}
```

#### GET /api/inventory-items/{id}

Returns one inventory item with reservations and stock movements.

#### GET /api/inventory-items/by-product/{productId}

Returns one inventory item for the product.

#### POST /api/inventory-items

```json
{
  "productId": "00000000-0000-0000-0000-000000000000",
  "sku": "SKU-001",
  "initialQuantity": 10
}
```

Success: `201 Created`

```json
{
  "id": "00000000-0000-0000-0000-000000000000"
}
```

#### POST /api/inventory-items/{id}/stock-increases

```json
{
  "quantity": 5,
  "reason": "Manual restock",
  "referenceId": "manual-001"
}
```

Success: `204 No Content`

#### PUT /api/inventory-items/{id}/stock-adjustment

```json
{
  "newTotalQuantity": 20,
  "reason": "Cycle count correction",
  "referenceId": "stock-count-001"
}
```

Success: `204 No Content`

## Checkout And Orders

Checkout requires `admin` or `customer`.

Important frontend rule: do not send `buyerId`. The authenticated user from the Keycloak token is the buyer. If `buyerId` is sent and it does not match the authenticated user, the backend rejects the request.

### POST /api/checkout/pay

Request:

```json
{
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000000",
      "sku": "SKU-001",
      "quantity": 2
    }
  ],
  "idempotencyKey": "checkout-cart-123",
  "provider": "Fake",
  "method": "Card"
}
```

Success: `201 Created`

```json
{
  "orderId": "00000000-0000-0000-0000-000000000000",
  "orderStatus": 1,
  "payment": {
    "paymentId": "00000000-0000-0000-0000-000000000000",
    "status": "RequiresAction",
    "provider": "Fake",
    "action": {
      "type": "Redirect",
      "redirectUrl": "/fake-3ds/payments/00000000-0000-0000-0000-000000000000",
      "clientSecret": null,
      "htmlContent": null
    }
  }
}
```

Frontend handling:

1. Persist `orderId` and `payment.paymentId` in UI state.
2. If `payment.action.type` is `Redirect`, navigate to or open `payment.action.redirectUrl` on the Gateway base URL.
3. For the local fake 3DS demo, complete the payment with `POST /fake-3ds/payments/{paymentId}/complete`.
4. Poll `GET /api/orders/{orderId}` until `status` becomes `2` Confirmed, `3` PaymentFailed, or `4` Failed.

### GET /api/orders/{orderId}

Roles: `admin`, `customer`

Returns order status and lines.

```json
{
  "orderId": "00000000-0000-0000-0000-000000000000",
  "buyerId": "00000000-0000-0000-0000-000000000000",
  "status": 2,
  "currency": "TRY",
  "totalAmount": 399.80,
  "createdAtUtc": "2026-06-07T07:30:49.814828Z",
  "updatedAtUtc": "2026-06-07T07:30:51.642068Z",
  "failureReason": null,
  "items": [
    {
      "orderLineId": "00000000-0000-0000-0000-000000000000",
      "productId": "00000000-0000-0000-0000-000000000000",
      "sku": "SKU-001",
      "productName": "Running Shoe",
      "variantName": "Default",
      "unitPrice": 199.90,
      "currency": "TRY",
      "quantity": 2,
      "lineTotal": 399.80
    }
  ]
}
```

Current limitation: Gateway enforces `customer` or `admin`, but Order service does not yet enforce owner-only order reads. Do not expose cross-user order browsing in the frontend demo.

## Payments

### GET /api/payments/{id}

Roles: `admin`, `customer`

Returns payment status.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "orderId": "00000000-0000-0000-0000-000000000000",
  "amount": 399.80,
  "currency": "TRY",
  "provider": 1,
  "method": 1,
  "status": 5,
  "idempotencyKey": "checkout-cart-123",
  "createdAtUtc": "2026-06-07T07:30:49.814828Z",
  "authorizedAtUtc": "2026-06-07T07:30:50.814828Z",
  "capturedAtUtc": "2026-06-07T07:30:51.814828Z",
  "refundedAtUtc": null,
  "failureReason": null
}
```

Current limitation: Gateway enforces `customer` or `admin`, but Payment service does not yet enforce owner-only payment reads. Prefer order polling for the customer checkout UI.

### Fake 3DS Demo

These endpoints are local-demo only and should not be treated as a real payment integration.

#### GET /fake-3ds/payments/{paymentId}

Public demo route.

```json
{
  "paymentId": "00000000-0000-0000-0000-000000000000",
  "completeUrl": "/fake-3ds/payments/00000000-0000-0000-0000-000000000000/complete"
}
```

#### POST /fake-3ds/payments/{paymentId}/complete

Public demo route.

```json
{
  "approved": true
}
```

Success: `200 OK`, returns payment status DTO.

Do not call provider callback endpoints from the frontend. They are intentionally not exposed through the Gateway route matrix.

## Notification Preferences

Notification preference endpoints require any authenticated user.

Current limitation: the frontend must pass `recipientId`; service-level recipient ownership is not enforced yet. For customer-facing screens, use the authenticated user's stable id only if the frontend can obtain it from the token claims. Otherwise keep this out of the customer demo.

### GET /api/notification-preferences?recipientId={recipientId}

Returns recipient preferences.

```json
{
  "id": "00000000-0000-0000-0000-000000000000",
  "recipientId": "customer1",
  "channel": 1,
  "notificationType": "OrderConfirmed",
  "isEnabled": true,
  "disabledReason": null
}
```

### PUT /api/notification-preferences/{recipientId}

```json
{
  "channel": 1,
  "notificationType": "OrderConfirmed",
  "isEnabled": true,
  "disabledReason": null
}
```

Success: `204 No Content`

## Notification Support

Notification support endpoints require `admin` or `support`.

### GET /api/notifications

Optional query parameters:

```text
recipient=<email-or-recipient>
status=<numeric NotificationMessageStatus>
notificationType=<type>
```

Returns notification list items.

### GET /api/notifications/{id}

Returns notification details including delivery attempts.

### POST /api/notifications/{id}/retry

Retries sending one notification.

Success: `200 OK`

```json
{
  "status": 1,
  "provider": "logging-email",
  "providerMessageId": "message-id",
  "errorMessage": null,
  "succeeded": true,
  "failed": false,
  "skipped": false
}
```

## Recommended Demo Flow

Use this sequence for the frontend demo:

1. Anonymous user browses `GET /api/products`, `GET /api/categories`, and `GET /api/brands`.
2. User signs in through Keycloak as `customer1`.
3. Frontend calls `POST /api/checkout/pay` without `buyerId`.
4. Frontend follows `payment.action.redirectUrl` for fake 3DS.
5. Frontend posts `{ "approved": true }` to the fake 3DS completion endpoint.
6. Frontend polls `GET /api/orders/{orderId}` until terminal status.
7. Admin/demo screens can use `admin`, `catalogmanager`, `inventorymanager`, or `support` accounts for protected operations.

## Local Verification Scripts

Backend smoke scripts that should pass before handing work to a frontend agent:

```bash
./scripts/smoke-gateway-auth.sh
./scripts/smoke-checkout.sh
```
