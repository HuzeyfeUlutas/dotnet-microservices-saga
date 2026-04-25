using MediatR;

namespace Catalog.Application.Features.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid ProductId) : IRequest;
