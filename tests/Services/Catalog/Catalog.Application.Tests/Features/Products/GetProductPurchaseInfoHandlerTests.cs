using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Features.Products.GetProductPurchaseInfo;
using Catalog.Application.Tests.Support;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Catalog.Application.Tests.Features.Products;

public class GetProductPurchaseInfoHandlerTests
{
    [Fact]
    public async Task Handle_should_return_purchasable_info_for_active_product_and_variant()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductWithVariantAsync(context);
        product.Activate();
        await context.SaveChangesAsync();
        var handler = new GetProductPurchaseInfoHandler(context);

        var result = await handler.Handle(
            new GetProductPurchaseInfoQuery(product.Id, " iphone15-128-black "),
            CancellationToken.None);

        result.ProductId.Should().Be(product.Id);
        result.ProductName.Should().Be("iPhone 15");
        result.Sku.Should().Be("IPHONE15-128-BLACK");
        result.VariantName.Should().Be("128GB Black");
        result.UnitPrice.Should().Be(49999.99m);
        result.Currency.Should().Be("TRY");
        result.ProductStatus.Should().Be(ProductStatus.Active);
        result.VariantStatus.Should().Be(VariantStatus.Active);
        result.IsPurchasable.Should().BeTrue();
        result.NotPurchasableReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_should_return_not_purchasable_when_product_is_not_active()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductWithVariantAsync(context);
        var handler = new GetProductPurchaseInfoHandler(context);

        var result = await handler.Handle(
            new GetProductPurchaseInfoQuery(product.Id, "IPHONE15-128-BLACK"),
            CancellationToken.None);

        result.IsPurchasable.Should().BeFalse();
        result.NotPurchasableReason.Should().Be("Product is not active.");
    }

    [Fact]
    public async Task Handle_should_return_not_purchasable_when_variant_is_not_active()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductWithVariantAsync(context);
        product.Activate();
        var variant = product.Variants.Should().ContainSingle().Subject;
        variant.Deactivate();
        await context.SaveChangesAsync();
        var handler = new GetProductPurchaseInfoHandler(context);

        var result = await handler.Handle(
            new GetProductPurchaseInfoQuery(product.Id, "IPHONE15-128-BLACK"),
            CancellationToken.None);

        result.IsPurchasable.Should().BeFalse();
        result.NotPurchasableReason.Should().Be("Product variant is not active.");
    }

    [Fact]
    public async Task Handle_should_return_not_purchasable_when_brand_is_not_active()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductWithVariantAsync(context);
        product.Activate();
        var brand = await context.Brands.FindAsync(product.BrandId);
        brand!.Deactivate();
        await context.SaveChangesAsync();
        var handler = new GetProductPurchaseInfoHandler(context);

        var result = await handler.Handle(
            new GetProductPurchaseInfoQuery(product.Id, "IPHONE15-128-BLACK"),
            CancellationToken.None);

        result.IsPurchasable.Should().BeFalse();
        result.NotPurchasableReason.Should().Be("Brand is not active.");
    }

    [Fact]
    public async Task Handle_should_return_not_purchasable_when_category_is_not_active()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductWithVariantAsync(context);
        product.Activate();
        var category = await context.Categories.FindAsync(product.CategoryId);
        category!.Deactivate();
        await context.SaveChangesAsync();
        var handler = new GetProductPurchaseInfoHandler(context);

        var result = await handler.Handle(
            new GetProductPurchaseInfoQuery(product.Id, "IPHONE15-128-BLACK"),
            CancellationToken.None);

        result.IsPurchasable.Should().BeFalse();
        result.NotPurchasableReason.Should().Be("Category is not active.");
    }

    [Fact]
    public async Task Handle_should_throw_not_found_when_sku_does_not_exist()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var product = await SeedProductWithVariantAsync(context);
        var handler = new GetProductPurchaseInfoHandler(context);

        var action = () => handler.Handle(
            new GetProductPurchaseInfoQuery(product.Id, "UNKNOWN-SKU"),
            CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Product variant with SKU 'UNKNOWN-SKU' was not found.");
    }

    private static async Task<Product> SeedProductWithVariantAsync(Catalog.Persistence.Context.CatalogDbContext context)
    {
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        var product = new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id);
        var variant = product.AddVariant("128GB Black", "IPHONE15-128-BLACK");

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        return product;
    }
}
