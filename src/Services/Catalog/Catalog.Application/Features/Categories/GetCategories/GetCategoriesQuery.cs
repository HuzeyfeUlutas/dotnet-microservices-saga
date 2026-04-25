using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Categories.GetCategories;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyCollection<CategoryListItemDto>>;
