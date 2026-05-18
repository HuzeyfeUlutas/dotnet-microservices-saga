namespace Order.Application.Abstractions.Services;

public sealed record CatalogPurchaseInfoDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    string VariantName,
    decimal UnitPrice,
    string Currency,
    string ProductStatus,
    string VariantStatus,
    Guid? BrandId,
    Guid? CategoryId,
    bool IsPurchasable,
    string? NotPurchasableReason);
