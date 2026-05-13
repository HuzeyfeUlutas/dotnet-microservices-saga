using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Features.Products.ActivateProductVariant;
using Catalog.Application.Features.Products.AddProductVariant;
using Catalog.Application.Features.Products.DeactivateProductVariant;
using Catalog.Application.Features.Products.UpdateProductVariant;
using Catalog.Application.Tests.Support;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Application.Tests.Features.Products;

public class ProductVariantHandlerTests
{
    [Fact]
    public async Task AddProductVariant_should_add_variant_to_product()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductAsync(context);
        var handler = new AddProductVariantHandler(context);

        var variantId = await handler.Handle(
            new AddProductVariantCommand(product.Id, "128GB Black", "IPHONE15-128-BLACK"),
            CancellationToken.None);

        var savedProduct = await context.Products
            .Include(x => x.Variants)
            .SingleAsync(x => x.Id == product.Id);
        var variant = savedProduct.Variants.Should().ContainSingle().Subject;
        variant.Id.Should().Be(variantId);
        variant.Name.Should().Be("128GB Black");
        variant.Sku.Should().Be("IPHONE15-128-BLACK");
        variant.Status.Should().Be(VariantStatus.Active);
    }

    [Fact]
    public async Task AddProductVariant_should_throw_conflict_when_sku_already_exists()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductAsync(context);
        var variant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        var handler = new AddProductVariantHandler(context);

        var action = () => handler.Handle(
            new AddProductVariantCommand(product.Id, "128GB Black Duplicate", "iphone15-128-black"),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Variant SKU 'iphone15-128-black' already exists for this product.");
    }

    [Fact]
    public async Task AddProductVariant_should_throw_conflict_when_product_is_archived()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductAsync(context);
        product.Archive();
        await context.SaveChangesAsync();
        var handler = new AddProductVariantHandler(context);

        var action = () => handler.Handle(
            new AddProductVariantCommand(product.Id, "128GB Black", "IPHONE15-128-BLACK"),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Variant cannot be added to an archived product.");
    }

    [Fact]
    public async Task UpdateProductVariant_should_update_variant_details()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductAsync(context);
        var variant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        var handler = new UpdateProductVariantHandler(context);

        await handler.Handle(
            new UpdateProductVariantCommand(product.Id, variant.Id, "256GB Blue", "IPHONE15-256-BLUE"),
            CancellationToken.None);

        variant.Name.Should().Be("256GB Blue");
        variant.Sku.Should().Be("IPHONE15-256-BLUE");
    }

    [Fact]
    public async Task UpdateProductVariant_should_throw_conflict_when_sku_belongs_to_another_variant()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductAsync(context);
        var firstVariant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");
        var secondVariant = product.AddVariant("256GB Blue", "IPHONE15-256-BLUE");
        context.ProductVariants.AddRange(firstVariant, secondVariant);
        await context.SaveChangesAsync();
        var handler = new UpdateProductVariantHandler(context);

        var action = () => handler.Handle(
            new UpdateProductVariantCommand(product.Id, firstVariant.Id, "128GB Black", "iphone15-256-blue"),
            CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Variant SKU 'iphone15-256-blue' already exists for this product.");
    }

    [Fact]
    public async Task ActivateProductVariant_should_activate_variant()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductAsync(context);
        var variant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");
        context.ProductVariants.Add(variant);
        variant.Deactivate();
        await context.SaveChangesAsync();
        var handler = new ActivateProductVariantHandler(context);

        await handler.Handle(new ActivateProductVariantCommand(product.Id, variant.Id), CancellationToken.None);

        variant.Status.Should().Be(VariantStatus.Active);
    }

    [Fact]
    public async Task DeactivateProductVariant_should_deactivate_variant()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductAsync(context);
        var variant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        var handler = new DeactivateProductVariantHandler(context);

        await handler.Handle(new DeactivateProductVariantCommand(product.Id, variant.Id), CancellationToken.None);

        variant.Status.Should().Be(VariantStatus.Inactive);
    }

    private static async Task<Product> SeedProductAsync(Catalog.Persistence.Context.CatalogDbContext context)
    {
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return product;
    }
}
