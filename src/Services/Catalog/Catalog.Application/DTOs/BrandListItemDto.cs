namespace Catalog.Application.DTOs;

public sealed record BrandListItemDto(
    Guid Id,
    string Name,
    bool IsActive);
