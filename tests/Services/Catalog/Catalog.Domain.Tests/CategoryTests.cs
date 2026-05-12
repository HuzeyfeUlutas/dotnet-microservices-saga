using System;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Catalog.Domain.Tests;

public class CategoryTests
{
    [Fact]
    public void Constructor_should_trim_name_and_description()
    {
        var category = new Category("  Phones  ", "  Smart phones  ");

        category.Name.Should().Be("Phones");
        category.Description.Should().Be("Smart phones");
        category.ParentCategoryId.Should().BeNull();
        category.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_should_reject_empty_name(string name)
    {
        var action = () => new Category(name);

        action.Should().Throw<DomainException>()
            .WithMessage("Category name cannot be empty.");
    }

    [Fact]
    public void ChangeParent_should_update_parent_category()
    {
        var parentId = Guid.NewGuid();
        var category = new Category("Phones");

        category.ChangeParent(parentId);

        category.ParentCategoryId.Should().Be(parentId);
    }

    [Fact]
    public void ChangeParent_should_reject_self_parent()
    {
        var category = new Category("Phones");

        var action = () => category.ChangeParent(category.Id);

        action.Should().Throw<DomainException>()
            .WithMessage("Category cannot be its own parent.");
    }

    [Fact]
    public void Activate_and_deactivate_should_change_active_state()
    {
        var category = new Category("Phones");

        category.Deactivate();
        category.IsActive.Should().BeFalse();

        category.Activate();
        category.IsActive.Should().BeTrue();
    }
}
