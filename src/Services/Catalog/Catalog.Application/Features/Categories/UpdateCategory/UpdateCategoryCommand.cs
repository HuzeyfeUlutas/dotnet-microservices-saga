using MediatR;

namespace Catalog.Application.Features.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool IsActive) : IRequest;
