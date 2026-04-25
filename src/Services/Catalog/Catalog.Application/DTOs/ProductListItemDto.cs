using Catalog.Domain.Enums;

namespace Catalog.Application.DTOs;

public sealed record ProductListItemDto(
    Guid Id,
    string Name,
    decimal Price,
    Guid BrandId,
    Guid CategoryId,
    ProductStatus Status);
