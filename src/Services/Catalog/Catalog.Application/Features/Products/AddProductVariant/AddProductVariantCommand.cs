using MediatR;

namespace Catalog.Application.Features.Products.AddProductVariant;

public sealed record AddProductVariantCommand(
    Guid ProductId,
    string Name,
    string Sku) : IRequest<Guid>;
