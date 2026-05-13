using MediatR;

namespace Catalog.Application.Features.Products.ActivateProductVariant;

public sealed record ActivateProductVariantCommand(
    Guid ProductId,
    Guid VariantId) : IRequest;
