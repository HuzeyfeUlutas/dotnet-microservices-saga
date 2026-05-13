using MediatR;

namespace Catalog.Application.Features.Products.UpdateProductVariant;

public sealed record UpdateProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string Name,
    string Sku) : IRequest;
