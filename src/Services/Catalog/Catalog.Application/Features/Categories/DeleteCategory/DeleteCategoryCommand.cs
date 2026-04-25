using MediatR;

namespace Catalog.Application.Features.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid CategoryId) : IRequest;
