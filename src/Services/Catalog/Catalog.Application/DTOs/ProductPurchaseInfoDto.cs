using Catalog.Domain.Enums;

namespace Catalog.Application.DTOs;

public sealed record ProductPurchaseInfoDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    string VariantName,
    decimal UnitPrice,
    string Currency,
    ProductStatus ProductStatus,
    VariantStatus VariantStatus,
    Guid BrandId,
    Guid CategoryId,
    bool IsPurchasable,
    string? NotPurchasableReason);
