using Catalog.Application.Abstractions.Messaging;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using Catalog.Domain.Entities;
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
        var product = await context.Products
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        }

        var brand = await context.Brands
            .FirstOrDefaultAsync(x => x.Id == request.BrandId, cancellationToken);
        if (brand is null)
        {
            throw new NotFoundException($"Brand '{request.BrandId}' was not found.");
        }

        if (!brand.IsActive)
        {
            throw new ConflictException("Product cannot use an inactive brand.");
        }

        var category = await context.Categories
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Category '{request.CategoryId}' was not found.");
        }

        if (!category.IsActive)
        {
            throw new ConflictException("Product cannot use an inactive category.");
        }

        var currentPrice = product.Price;
        var currentStatus = product.Status;

        if (currentStatus == ProductStatus.Archived && request.Status == ProductStatus.Active)
        {
            throw new ConflictException("Archived product cannot be reactivated.");
        }

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
                    "Deactivated",
                    BuildVariantSnapshots(product)),
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

    private static IReadOnlyCollection<ProductUnavailableVariantSnapshot> BuildVariantSnapshots(Product product)
    {
        return product.Variants
            .Select(x => new ProductUnavailableVariantSnapshot(x.Id, x.Sku))
            .ToArray();
    }
}
