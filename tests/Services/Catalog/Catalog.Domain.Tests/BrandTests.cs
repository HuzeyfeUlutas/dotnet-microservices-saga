using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalog.Domain.Tests;

public class BrandTests
{
    [Fact]
    public void Constructor_should_trim_name_and_description()
    {
        var brand = new Brand("  Apple  ", "  Consumer electronics  ");

        brand.Name.Should().Be("Apple");
        brand.Description.Should().Be("Consumer electronics");
        brand.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_should_reject_empty_name(string name)
    {
        var action = () => new Brand(name);

        action.Should().Throw<DomainException>()
            .WithMessage("Brand name cannot be empty.");
    }

    [Fact]
    public void UpdateDetails_should_trim_values()
    {
        var brand = new Brand("Apple");

        brand.UpdateDetails("  Samsung  ", "  Mobile devices  ");

        brand.Name.Should().Be("Samsung");
        brand.Description.Should().Be("Mobile devices");
    }

    [Fact]
    public void Activate_and_deactivate_should_change_active_state()
    {
        var brand = new Brand("Apple");

        brand.Deactivate();
        brand.IsActive.Should().BeFalse();

        brand.Activate();
        brand.IsActive.Should().BeTrue();
    }
}
