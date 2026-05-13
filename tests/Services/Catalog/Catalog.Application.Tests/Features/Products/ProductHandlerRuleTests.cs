using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Features.Products.CreateProduct;
using Catalog.Application.Features.Products.UpdateProduct;
using Catalog.Application.Tests.Support;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Catalog.Application.Tests.Features.Products;

public class ProductHandlerRuleTests
{
    [Fact]
    public async Task CreateProduct_should_throw_conflict_when_brand_is_inactive()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var brand = new Brand("Apple");
        brand.Deactivate();
        var category = new Category("Phones");
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var handler = new CreateProductHandler(
            context,
            new CapturingIntegrationEventPublisher(),
            Substitute.For<ICatalogMetrics>(),
            NullLogger<CreateProductHandler>.Instance);

        var action = () => handler.Handle(
            new CreateProductCommand("iPhone 15", null, 49999.99m, brand.Id, category.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Product cannot use an inactive brand.");
    }

    [Fact]
    public async Task CreateProduct_should_throw_conflict_when_category_is_inactive()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var brand = new Brand("Apple");
        var category = new Category("Phones");
        category.Deactivate();
        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var handler = new CreateProductHandler(
            context,
            new CapturingIntegrationEventPublisher(),
            Substitute.For<ICatalogMetrics>(),
            NullLogger<CreateProductHandler>.Instance);

        var action = () => handler.Handle(
            new CreateProductCommand("iPhone 15", null, 49999.99m, brand.Id, category.Id),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Product cannot use an inactive category.");
    }

    [Fact]
    public async Task UpdateProduct_should_throw_conflict_when_brand_is_inactive()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var inactiveBrand = new Brand("Samsung");
        inactiveBrand.Deactivate();
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        context.Brands.Add(inactiveBrand);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var handler = new UpdateProductHandler(
            context,
            new CapturingIntegrationEventPublisher(),
            Substitute.For<ICatalogMetrics>(),
            NullLogger<UpdateProductHandler>.Instance);

        var action = () => handler.Handle(
            new UpdateProductCommand(product.Id, "iPhone 15", null, 49999.99m, inactiveBrand.Id, category.Id, ProductStatus.Active),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Product cannot use an inactive brand.");
    }

    [Fact]
    public async Task UpdateProduct_should_throw_conflict_when_category_is_inactive()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var inactiveCategory = new Category("Laptops");
        inactiveCategory.Deactivate();
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        context.Categories.Add(inactiveCategory);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var handler = new UpdateProductHandler(
            context,
            new CapturingIntegrationEventPublisher(),
            Substitute.For<ICatalogMetrics>(),
            NullLogger<UpdateProductHandler>.Instance);

        var action = () => handler.Handle(
            new UpdateProductCommand(product.Id, "iPhone 15", null, 49999.99m, brand.Id, inactiveCategory.Id, ProductStatus.Active),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Product cannot use an inactive category.");
    }

    [Fact]
    public async Task UpdateProduct_should_throw_conflict_when_archived_product_is_reactivated()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        product.Archive();
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var handler = new UpdateProductHandler(
            context,
            new CapturingIntegrationEventPublisher(),
            Substitute.For<ICatalogMetrics>(),
            NullLogger<UpdateProductHandler>.Instance);

        var action = () => handler.Handle(
            new UpdateProductCommand(product.Id, "iPhone 15", null, 49999.99m, brand.Id, category.Id, ProductStatus.Active),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Archived product cannot be reactivated.");
    }
}
