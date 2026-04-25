using Catalog.Application.DTOs;
using MediatR;

namespace Catalog.Application.Features.Products.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto>;
