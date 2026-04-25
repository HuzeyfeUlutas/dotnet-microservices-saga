namespace Catalog.Application.DTOs;

public sealed record ProductImageDto(
    Guid Id,
    string ImageUrl,
    string? AltText,
    int SortOrder,
    bool IsPrimary);
