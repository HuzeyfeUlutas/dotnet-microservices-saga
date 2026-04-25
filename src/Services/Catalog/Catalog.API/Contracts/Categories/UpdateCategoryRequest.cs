namespace Catalog.API.Contracts.Categories;

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive);
