using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Products.GetProducts;

public sealed record GetProductsQuery : IRequest<IReadOnlyCollection<ProductListItemDto>>;
