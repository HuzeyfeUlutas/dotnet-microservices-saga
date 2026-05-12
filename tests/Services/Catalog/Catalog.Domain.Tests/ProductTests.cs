using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalog.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Constructor_should_initialize_product_as_draft()
    {
        var brandId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var product = new Product("  iPhone 15  ", "  Smartphone  ", 49999.99m, brandId, categoryId);

        product.Name.Should().Be("iPhone 15");
        product.Description.Should().Be("Smartphone");
        product.Price.Should().Be(49999.99m);
        product.BrandId.Should().Be(brandId);
        product.CategoryId.Should().Be(categoryId);
        product.Status.Should().Be(ProductStatus.Draft);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_should_reject_empty_name(string name)
    {
        var action = () => new Product(name, null, 10m, Guid.NewGuid(), Guid.NewGuid());

        action.Should().Throw<DomainException>()
            .WithMessage("Product name cannot be empty.");
    }

    [Fact]
    public void Constructor_should_reject_negative_price()
    {
        var action = () => new Product("iPhone 15", null, -1m, Guid.NewGuid(), Guid.NewGuid());

        action.Should().Throw<DomainException>()
            .WithMessage("Price cannot be negative.");
    }

    [Fact]
    public void Constructor_should_reject_empty_brand_id()
    {
        var action = () => new Product("iPhone 15", null, 10m, Guid.Empty, Guid.NewGuid());

        action.Should().Throw<DomainException>()
            .WithMessage("Brand id cannot be empty.");
    }

    [Fact]
    public void Constructor_should_reject_empty_category_id()
    {
        var action = () => new Product("iPhone 15", null, 10m, Guid.NewGuid(), Guid.Empty);

        action.Should().Throw<DomainException>()
            .WithMessage("Category id cannot be empty.");
    }

    [Fact]
    public void ChangePrice_should_update_price()
    {
        var product = CreateProduct();

        product.ChangePrice(41999.99m);

        product.Price.Should().Be(41999.99m);
    }

    [Fact]
    public void Status_methods_should_update_product_status()
    {
        var product = CreateProduct();

        product.Activate();
        product.Status.Should().Be(ProductStatus.Active);

        product.Deactivate();
        product.Status.Should().Be(ProductStatus.Inactive);

        product.Archive();
        product.Status.Should().Be(ProductStatus.Archived);
    }

    [Fact]
    public void AddVariant_should_reject_duplicate_sku_within_product()
    {
        var product = CreateProduct();
        product.AddVariant("128GB Black", "IPHONE15-128-BLACK");

        var action = () => product.AddVariant("128GB Black Duplicate", "iphone15-128-black");

        action.Should().Throw<DomainException>()
            .WithMessage("Variant SKU must be unique within the product.");
    }

    [Fact]
    public void AddImage_should_keep_only_one_primary_image()
    {
        var product = CreateProduct();

        var firstImage = product.AddImage("https://cdn.example.com/iphone-front.jpg", "Front", 0, true);
        var secondImage = product.AddImage("https://cdn.example.com/iphone-back.jpg", "Back", 1, true);

        firstImage.IsPrimary.Should().BeFalse();
        secondImage.IsPrimary.Should().BeTrue();
        product.Images.Should().ContainSingle(x => x.IsPrimary);
    }

    private static Product CreateProduct()
    {
        return new Product("iPhone 15", "Smartphone", 49999.99m, Guid.NewGuid(), Guid.NewGuid());
    }
}
