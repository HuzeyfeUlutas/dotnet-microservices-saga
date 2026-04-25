using Catalog.Domain.Enums;
using MediatR;

namespace Catalog.Application.Features.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    decimal Price,
    Guid BrandId,
    Guid CategoryId,
    ProductStatus Status) : IRequest;
