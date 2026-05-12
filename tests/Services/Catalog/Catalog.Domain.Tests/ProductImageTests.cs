using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalog.Domain.Tests;

public class ProductImageTests
{
    [Fact]
    public void Constructor_should_trim_values()
    {
        var productId = Guid.NewGuid();

        var image = new ProductImage(productId, "  https://cdn.example.com/image.jpg  ", "  Main image  ", 1, true);

        image.ProductId.Should().Be(productId);
        image.ImageUrl.Should().Be("https://cdn.example.com/image.jpg");
        image.AltText.Should().Be("Main image");
        image.SortOrder.Should().Be(1);
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Constructor_should_reject_empty_product_id()
    {
        var action = () => new ProductImage(Guid.Empty, "https://cdn.example.com/image.jpg", null, 0, false);

        action.Should().Throw<DomainException>()
            .WithMessage("Product id cannot be empty.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_should_reject_empty_image_url(string imageUrl)
    {
        var action = () => new ProductImage(Guid.NewGuid(), imageUrl, null, 0, false);

        action.Should().Throw<DomainException>()
            .WithMessage("Image URL cannot be empty.");
    }

    [Fact]
    public void Constructor_should_reject_negative_sort_order()
    {
        var action = () => new ProductImage(Guid.NewGuid(), "https://cdn.example.com/image.jpg", null, -1, false);

        action.Should().Throw<DomainException>()
            .WithMessage("Sort order cannot be negative.");
    }

    [Fact]
    public void MarkAsPrimary_and_clear_primary_should_update_primary_state()
    {
        var image = new ProductImage(Guid.NewGuid(), "https://cdn.example.com/image.jpg", null, 0, false);

        image.MarkAsPrimary();
        image.IsPrimary.Should().BeTrue();

        image.ClearPrimary();
        image.IsPrimary.Should().BeFalse();
    }
}
