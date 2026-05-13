# Inventory Reservation HTTP Contract

This document defines the internal HTTP contract for Inventory reservation operations.

These endpoints are intended for service-to-service usage such as `Order -> Inventory`.
They are not frontend-facing endpoints.

## Endpoints

### Reserve stock

`POST /api/reservations`

Request body:

```json
{
  "productId": "00000000-0000-0000-0000-000000000000",
  "sku": "SKU-001",
  "orderId": "00000000-0000-0000-0000-000000000000",
  "quantity": 1,
  "expiresAtUtc": "2026-05-13T12:00:00Z"
}
```

Success response:

```json
{
  "reservationId": "00000000-0000-0000-0000-000000000000",
  "inventoryItemId": "00000000-0000-0000-0000-000000000000",
  "productId": "00000000-0000-0000-0000-000000000000",
  "sku": "SKU-001",
  "orderId": "00000000-0000-0000-0000-000000000000",
  "quantity": 1,
  "status": "Pending",
  "expiresAtUtc": "2026-05-13T12:00:00Z"
}
```

### Commit reservation

`POST /api/reservations/commit`

Request body:

```json
{
  "productId": "00000000-0000-0000-0000-000000000000",
  "sku": "SKU-001",
  "orderId": "00000000-0000-0000-0000-000000000000"
}
```

Success response:

`204 No Content`

### Release reservation

`POST /api/reservations/release`

Request body:

```json
{
  "productId": "00000000-0000-0000-0000-000000000000",
  "sku": "SKU-001",
  "orderId": "00000000-0000-0000-0000-000000000000"
}
```

Success response:

`204 No Content`

## Lookup Rule

Reservation operations must target the inventory item by:

`ProductId + Sku`

Do not use only `ProductId` for checkout reservation flows.

## Error Semantics

- `400 Bad Request`: invalid request payload or domain rule violation
- `404 Not Found`: inventory item or reservation not found
- `409 Conflict`: insufficient stock, invalid state transition, or concurrency conflict

## Idempotency Direction

- Reserve must be idempotent for the same `OrderId + ProductId + Sku`
- Commit must be idempotent for repeated requests on the same reservation flow
- Release must be idempotent for repeated requests on the same reservation flow
