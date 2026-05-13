namespace Catalog.API.Contracts.Products;

public sealed record AddProductVariantRequest(
    string Name,
    string Sku);
