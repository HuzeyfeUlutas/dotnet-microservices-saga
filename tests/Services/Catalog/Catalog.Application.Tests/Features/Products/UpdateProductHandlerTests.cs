using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using Catalog.Application.Features.Products.UpdateProduct;
using Catalog.Application.Tests.Support;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Catalog.Application.Tests.Features.Products;

public class UpdateProductHandlerTests
{
    [Fact]
    public async Task Handle_should_publish_price_updated_event_when_price_changes()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var publisher = new CapturingIntegrationEventPublisher();
        var metrics = Substitute.For<ICatalogMetrics>();
        var handler = new UpdateProductHandler(context, publisher, metrics, NullLogger<UpdateProductHandler>.Instance);

        await handler.Handle(
            new UpdateProductCommand(product.Id, "iPhone 15", "Updated", 45999.99m, brand.Id, category.Id, ProductStatus.Active),
            CancellationToken.None);

        var updatedProduct = await context.Products.SingleAsync(x => x.Id == product.Id);
        updatedProduct.Price.Should().Be(45999.99m);
        updatedProduct.Status.Should().Be(ProductStatus.Active);

        var publishedEvent = publisher.PublishedMessages
            .Should().ContainSingle()
            .Subject.Should().BeOfType<ProductPriceUpdatedIntegrationEvent>()
            .Subject;
        publishedEvent.ProductId.Should().Be(product.Id);
        publishedEvent.OldPrice.Should().Be(49999.99m);
        publishedEvent.NewPrice.Should().Be(45999.99m);

        metrics.Received(1).RecordProductPriceUpdated();
    }

    [Fact]
    public async Task Handle_should_publish_unavailable_event_when_product_becomes_inactive()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        product.Activate();
        var variant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        var publisher = new CapturingIntegrationEventPublisher();
        var metrics = Substitute.For<ICatalogMetrics>();
        var handler = new UpdateProductHandler(context, publisher, metrics, NullLogger<UpdateProductHandler>.Instance);

        await handler.Handle(
            new UpdateProductCommand(product.Id, "iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id, ProductStatus.Inactive),
            CancellationToken.None);

        var publishedEvent = publisher.PublishedMessages
            .Should().ContainSingle()
            .Subject.Should().BeOfType<ProductUnavailableIntegrationEvent>()
            .Subject;
        publishedEvent.ProductId.Should().Be(product.Id);
        publishedEvent.Reason.Should().Be("Deactivated");
        var variantSnapshot = publishedEvent.Variants.Should().ContainSingle().Subject;
        variantSnapshot.VariantId.Should().Be(variant.Id);
        variantSnapshot.Sku.Should().Be("IPHONE15-128-BLACK");

        metrics.Received(1).RecordProductUnavailable("Deactivated");
    }
}
