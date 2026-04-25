namespace Catalog.Application.DTOs;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);
