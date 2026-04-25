namespace Catalog.API.Contracts.Categories;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description,
    Guid? ParentCategoryId);
