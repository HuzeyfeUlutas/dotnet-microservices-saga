using Catalog.Application.Abstractions.Messaging;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Products.DeactivateProductVariant;

public class DeactivateProductVariantHandler(
    ICatalogDbContext context,
    IIntegrationEventPublisher eventPublisher,
    ICatalogMetrics metrics,
    ILogger<DeactivateProductVariantHandler> logger)
    : IRequestHandler<DeactivateProductVariantCommand>
{
    public async Task Handle(DeactivateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        var variant = product.Variants.SingleOrDefault(x => x.Id == request.VariantId);
        if (variant is null)
        {
            throw new NotFoundException($"Product variant '{request.VariantId}' was not found.");
        }

        variant.Deactivate();
        await eventPublisher.PublishAsync(
            new ProductVariantUnavailableIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                product.Id,
                variant.Id,
                variant.Sku,
                "VariantDeactivated"),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordProductVariantUnavailable("VariantDeactivated");
        logger.LogInformation(
            "Product variant deactivated for {ProductId}, {VariantId} and {Sku}",
            product.Id,
            variant.Id,
            variant.Sku);
    }
}
