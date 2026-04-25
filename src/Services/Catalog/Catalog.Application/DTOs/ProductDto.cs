using Catalog.Domain.Enums;

namespace Catalog.Application.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    Guid BrandId,
    Guid CategoryId,
    ProductStatus Status,
    IReadOnlyCollection<ProductVariantDto> Variants,
    IReadOnlyCollection<ProductImageDto> Images);
