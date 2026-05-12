using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Contracts.IntegrationEvents.Products;
using Catalog.Application.Features.Products.CreateProduct;
using Catalog.Application.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Catalog.Application.Tests.Features.Products;

public class CreateProductHandlerTests
{
    [Fact]
    public async Task Handle_should_create_product_and_publish_product_created_event()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var publisher = new CapturingIntegrationEventPublisher();
        var metrics = Substitute.For<ICatalogMetrics>();
        var handler = new CreateProductHandler(context, publisher, metrics, NullLogger<CreateProductHandler>.Instance);

        var productId = await handler.Handle(
            new CreateProductCommand("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id),
            CancellationToken.None);

        var product = await context.Products.SingleAsync(x => x.Id == productId);
        product.Name.Should().Be("iPhone 15");
        product.Price.Should().Be(49999.99m);
        product.BrandId.Should().Be(brand.Id);
        product.CategoryId.Should().Be(category.Id);

        var publishedEvent = publisher.PublishedMessages
            .Should().ContainSingle()
            .Subject.Should().BeOfType<ProductCreatedIntegrationEvent>()
            .Subject;
        publishedEvent.ProductId.Should().Be(productId);
        publishedEvent.Name.Should().Be("iPhone 15");
        publishedEvent.Price.Should().Be(49999.99m);
        publishedEvent.BrandId.Should().Be(brand.Id);
        publishedEvent.CategoryId.Should().Be(category.Id);

        metrics.Received(1).RecordProductCreated();
    }

    [Fact]
    public async Task Handle_should_throw_not_found_when_brand_does_not_exist()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var category = new Catalog.Domain.Entities.Category("Phones");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var handler = new CreateProductHandler(
            context,
            new CapturingIntegrationEventPublisher(),
            Substitute.For<ICatalogMetrics>(),
            NullLogger<CreateProductHandler>.Instance);

        var action = () => handler.Handle(
            new CreateProductCommand("iPhone 15", null, 49999.99m, Guid.NewGuid(), category.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Brand '*' was not found.");
    }

    [Fact]
    public async Task Handle_should_throw_not_found_when_category_does_not_exist()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var brand = new Catalog.Domain.Entities.Brand("Apple");
        context.Brands.Add(brand);
        await context.SaveChangesAsync();
        var handler = new CreateProductHandler(
            context,
            new CapturingIntegrationEventPublisher(),
            Substitute.For<ICatalogMetrics>(),
            NullLogger<CreateProductHandler>.Instance);

        var action = () => handler.Handle(
            new CreateProductCommand("iPhone 15", null, 49999.99m, brand.Id, Guid.NewGuid()),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Category '*' was not found.");
    }
}
