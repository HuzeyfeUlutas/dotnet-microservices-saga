using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Categories.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid CategoryId) : IRequest<CategoryDto>;
