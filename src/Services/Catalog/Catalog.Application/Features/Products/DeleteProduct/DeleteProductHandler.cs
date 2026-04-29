using Catalog.Application.Abstractions.Messaging;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Products.DeleteProduct;

public class DeleteProductHandler(
    ICatalogDbContext context,
    IIntegrationEventPublisher eventPublisher,
    ICatalogMetrics metrics,
    ILogger<DeleteProductHandler> logger)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        product.MarkAsDeleted();
        await eventPublisher.PublishAsync(
            new ProductUnavailableIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                product.Id,
                "Deleted"),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordProductUnavailable("Deleted");
        logger.LogInformation(
            "Product marked as deleted for {ProductId}",
            product.Id);
    }
}
