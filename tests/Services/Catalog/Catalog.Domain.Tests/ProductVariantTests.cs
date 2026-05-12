using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalog.Domain.Tests;

public class ProductVariantTests
{
    [Fact]
    public void Constructor_should_trim_values_and_activate_variant()
    {
        var productId = Guid.NewGuid();

        var variant = new ProductVariant(productId, "  128GB Black  ", "  IPHONE15-128-BLACK  ");

        variant.ProductId.Should().Be(productId);
        variant.Name.Should().Be("128GB Black");
        variant.Sku.Should().Be("IPHONE15-128-BLACK");
        variant.Status.Should().Be(VariantStatus.Active);
    }

    [Fact]
    public void Constructor_should_reject_empty_product_id()
    {
        var action = () => new ProductVariant(Guid.Empty, "128GB Black", "IPHONE15-128-BLACK");

        action.Should().Throw<DomainException>()
            .WithMessage("Product id cannot be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_should_reject_empty_name(string name)
    {
        var action = () => new ProductVariant(Guid.NewGuid(), name, "IPHONE15-128-BLACK");

        action.Should().Throw<DomainException>()
            .WithMessage("Variant name cannot be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_should_reject_empty_sku(string sku)
    {
        var action = () => new ProductVariant(Guid.NewGuid(), "128GB Black", sku);

        action.Should().Throw<DomainException>()
            .WithMessage("Variant SKU cannot be empty.");
    }

    [Fact]
    public void Activate_and_deactivate_should_change_variant_status()
    {
        var variant = new ProductVariant(Guid.NewGuid(), "128GB Black", "IPHONE15-128-BLACK");

        variant.Deactivate();
        variant.Status.Should().Be(VariantStatus.Inactive);

        variant.Activate();
        variant.Status.Should().Be(VariantStatus.Active);
    }
}
