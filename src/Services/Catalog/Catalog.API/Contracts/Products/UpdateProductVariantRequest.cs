namespace Catalog.API.Contracts.Products;

public sealed record UpdateProductVariantRequest(
    string Name,
    string Sku);
