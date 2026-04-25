namespace Catalog.API.Contracts.Products;

public sealed record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    Guid BrandId,
    Guid CategoryId);
