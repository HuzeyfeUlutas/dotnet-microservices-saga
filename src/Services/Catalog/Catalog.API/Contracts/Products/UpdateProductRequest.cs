using Catalog.Domain.Enums;

namespace Catalog.API.Contracts.Products;

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    Guid BrandId,
    Guid CategoryId,
    ProductStatus Status);
