using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using Catalog.Application.Features.Products.DeleteProduct;
using Catalog.Application.Tests.Support;
using Catalog.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Catalog.Application.Tests.Features.Products;

public class DeleteProductHandlerTests
{
    [Fact]
    public async Task Handle_should_soft_delete_product_and_publish_unavailable_event()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        var variant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        var publisher = new CapturingIntegrationEventPublisher();
        var metrics = Substitute.For<ICatalogMetrics>();
        var handler = new DeleteProductHandler(context, publisher, metrics, NullLogger<DeleteProductHandler>.Instance);

        await handler.Handle(new DeleteProductCommand(product.Id), CancellationToken.None);

        var visibleProduct = await context.Products.FirstOrDefaultAsync(x => x.Id == product.Id);
        visibleProduct.Should().BeNull();

        var deletedProduct = await context.Products
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == product.Id);
        deletedProduct.IsDeleted.Should().BeTrue();

        var publishedEvent = publisher.PublishedMessages
            .Should().ContainSingle()
            .Subject.Should().BeOfType<ProductUnavailableIntegrationEvent>()
            .Subject;
        publishedEvent.ProductId.Should().Be(product.Id);
        publishedEvent.Reason.Should().Be("Deleted");
        var variantSnapshot = publishedEvent.Variants.Should().ContainSingle().Subject;
        variantSnapshot.VariantId.Should().Be(variant.Id);
        variantSnapshot.Sku.Should().Be("IPHONE15-128-BLACK");

        metrics.Received(1).RecordProductUnavailable("Deleted");
    }
}
