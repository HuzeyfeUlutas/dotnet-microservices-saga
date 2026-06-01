# Catalog Order Readiness

This document captures the Catalog decisions made before implementing Order service flows.

## Current Status

Catalog is ready to be used by Order as a product snapshot source.

Validated locally:

```text
dotnet ef migrations has-pending-model-changes
dotnet test MarketplaceOrderPlatform.sln -m:1 -nr:false
dotnet build MarketplaceOrderPlatform.sln -m:1 -nr:false
```

Known non-blocking warning:

```text
NU1902 for OpenTelemetry.Exporter.OpenTelemetryProtocol 1.15.2
```

Treat this as a separate dependency security task, not an Order-blocking Catalog issue.

## Purchase Info Contract

The current implementation exposes product snapshot data over internal HTTP:

```text
GET /api/products/{productId}/purchase-info?sku={sku}
```

This HTTP endpoint is internal-only and will be removed after Order migrates to the Catalog-owned gRPC method:

```text
GetPurchaseInfo(ProductId, Sku)
```

The gRPC method must preserve the same response fields and Application query behavior.

The response is intended to provide:

```text
ProductId
ProductName
Sku
VariantName
UnitPrice
Currency
ProductStatus
VariantStatus
BrandId
CategoryId
IsPurchasable
NotPurchasableReason
```

Order should persist its own order line snapshot instead of depending on Catalog data staying unchanged.

Recommended Order line identity:

```text
OrderLineId
OrderId
ProductId
Sku
ProductName
VariantName
UnitPrice
Currency
Quantity
```

## Availability Boundary

Catalog does not own stock.

Catalog purchasability covers:

```text
product status
variant status
brand active state
category active state
```

Inventory must still validate and reserve stock during checkout.

## Integration Events

Catalog publishes these product events:

```text
ProductCreatedIntegrationEvent
ProductPriceUpdatedIntegrationEvent
ProductUnavailableIntegrationEvent
ProductVariantUnavailableIntegrationEvent
```

Unavailable event rules:

- Product-level unavailable events include affected variant/SKU snapshots.
- Variant-level unavailable events include `ProductId`, `VariantId`, `Sku`, and reason.
- Downstream consumers can use these events to invalidate Redis/product availability cache without querying Catalog.

## Persistence and Seed

Catalog uses Code First migrations.

Relevant migrations added for Order readiness:

```text
SeedCatalogData
AddCatalogUniquenessIndexes
```

Apply Catalog migrations with:

```bash
dotnet ef database update \
  --project src/Services/Catalog/Catalog.Persistence/Catalog.Persistence.csproj \
  --startup-project src/Services/Catalog/Catalog.API/Catalog.API.csproj \
  --context CatalogDbContext
```

Seed data includes active products, inactive variant, archived product, inactive brand, and inactive category scenarios.

## Consistency Rules

Catalog protects duplicate names/SKUs with application checks and database unique indexes.

Current DB-level uniqueness guards:

```text
Brand name case-insensitive unique among non-deleted rows
Category name case-insensitive unique among non-deleted rows
Product variant SKU case-insensitive unique per product among non-deleted rows
```

PostgreSQL unique violations are mapped to HTTP `409 Conflict`.

## Observability

Catalog exposes:

```text
GET /health/live
GET /health/ready
GET /metrics
```

Readiness checks include:

```text
PostgreSQL
RabbitMQ
Elasticsearch
```

Health responses are JSON and include dependency status details.
