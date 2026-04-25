using MediatR;

namespace Catalog.Application.Features.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    Guid? ParentCategoryId) : IRequest<Guid>;
