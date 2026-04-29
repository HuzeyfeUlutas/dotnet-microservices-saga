using Catalog.Application.Abstractions.Messaging;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Abstractions.Persistence;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using Catalog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Features.Products.CreateProduct;

// Product olusturma use-case'ini tek transaction icinde yurutur.
public class CreateProductHandler(
    ICatalogDbContext context,
    IIntegrationEventPublisher eventPublisher,
    ICatalogMetrics metrics,
    ILogger<CreateProductHandler> logger)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
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

        var product = new Product(
            request.Name,
            request.Description,
            request.Price,
            request.BrandId,
            request.CategoryId);

        context.Products.Add(product);
        await eventPublisher.PublishAsync(
            new ProductCreatedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                product.Id,
                product.Name,
                product.Price,
                product.BrandId,
                product.CategoryId),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        metrics.RecordProductCreated();
        logger.LogInformation(
            "Product created for {ProductId} with BrandId {BrandId}, CategoryId {CategoryId} and Price {Price}",
            product.Id,
            product.BrandId,
            product.CategoryId,
            product.Price);

        return product.Id;
    }
}
