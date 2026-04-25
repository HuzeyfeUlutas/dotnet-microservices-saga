namespace Catalog.Application.DTOs;

public sealed record CategoryListItemDto(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    bool IsActive);
