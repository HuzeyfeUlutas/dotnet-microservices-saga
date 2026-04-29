using Catalog.Application.Abstractions.Messaging;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using Catalog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Products.UpdateProduct;

public class UpdateProductHandler(
    ICatalogDbContext context,
    IIntegrationEventPublisher eventPublisher,
    ICatalogMetrics metrics,
    ILogger<UpdateProductHandler> logger)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        var brandExists = await context.Brands.AnyAsync(x => x.Id == request.BrandId, cancellationToken);
        if (!brandExists)
        {
            throw new NotFoundException($"Brand '{request.BrandId}' was not found.");
        }

        var categoryExists = await context.Categories.AnyAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        var currentPrice = product.Price;
        var currentStatus = product.Status;

        product.UpdateDetails(request.Name, request.Description);
        product.ChangePrice(request.Price);
        product.ChangeBrand(request.BrandId);
        product.ChangeCategory(request.CategoryId);

        switch (request.Status)
        {
            case ProductStatus.Active:
                product.Activate();
                break;
            case ProductStatus.Inactive:
                product.Deactivate();
                break;
            case ProductStatus.Archived:
                product.Archive();
                break;
            default:
                break;
        }

        if (currentPrice != product.Price)
        {
            await eventPublisher.PublishAsync(
                new ProductPriceUpdatedIntegrationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    product.Id,
                    currentPrice,
                    product.Price),
                cancellationToken);
        }

        var becameUnavailable = currentStatus != ProductStatus.Inactive && product.Status == ProductStatus.Inactive;

        if (becameUnavailable)
        {
            await eventPublisher.PublishAsync(
                new ProductUnavailableIntegrationEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    product.Id,
                    "Deactivated"),
                cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (currentPrice != product.Price)
        {
            metrics.RecordProductPriceUpdated();
            logger.LogInformation(
                "Product price updated for {ProductId} from {PreviousPrice} to {CurrentPrice}",
                product.Id,
                currentPrice,
                product.Price);
        }

        if (becameUnavailable)
        {
            metrics.RecordProductUnavailable("Deactivated");
            logger.LogInformation(
                "Product deactivated for {ProductId}",
                product.Id);
        }
    }
}
