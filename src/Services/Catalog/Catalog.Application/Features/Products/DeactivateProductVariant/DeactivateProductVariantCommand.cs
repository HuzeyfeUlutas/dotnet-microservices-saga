using MediatR;

namespace Catalog.Application.Features.Products.DeactivateProductVariant;

public sealed record DeactivateProductVariantCommand(
    Guid ProductId,
    Guid VariantId) : IRequest;
