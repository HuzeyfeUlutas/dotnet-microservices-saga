using System;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Features.Categories.CreateCategory;
using Catalog.Application.Features.Categories.DeleteCategory;
using Catalog.Application.Features.Categories.UpdateCategory;
using Catalog.Application.Tests.Support;
using Catalog.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Catalog.Application.Tests.Features.Categories;

public class CategoryHandlerRuleTests
{
    [Fact]
    public async Task CreateCategory_should_throw_conflict_when_name_already_exists()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        context.Categories.Add(new Category("Phones"));
        await context.SaveChangesAsync();
        var handler = new CreateCategoryHandler(context, Substitute.For<ICatalogMetrics>(), NullLogger<CreateCategoryHandler>.Instance);

        var action = () => handler.Handle(new CreateCategoryCommand(" phones ", null, null), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Category name 'phones' already exists.");
    }

    [Fact]
    public async Task UpdateCategory_should_throw_conflict_when_name_belongs_to_another_category()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var phones = new Category("Phones");
        var laptops = new Category("Laptops");
        context.Categories.AddRange(phones, laptops);
        await context.SaveChangesAsync();
        var handler = new UpdateCategoryHandler(context, Substitute.For<ICatalogMetrics>(), NullLogger<UpdateCategoryHandler>.Instance);

        var action = () => handler.Handle(new UpdateCategoryCommand(laptops.Id, " phones ", null, null, true), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Category name 'phones' already exists.");
    }

    [Fact]
    public async Task DeleteCategory_should_throw_conflict_when_category_has_products()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        context.Products.Add(new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id));
        await context.SaveChangesAsync();
        var handler = new DeleteCategoryHandler(context, Substitute.For<ICatalogMetrics>(), NullLogger<DeleteCategoryHandler>.Instance);

        var action = () => handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Category cannot be deleted because it has products.");
    }

    [Fact]
    public async Task DeleteCategory_should_throw_conflict_when_category_has_child_categories()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var parent = new Category("Electronics");
        var child = new Category("Phones", parentCategoryId: parent.Id);
        context.Categories.AddRange(parent, child);
        await context.SaveChangesAsync();
        var handler = new DeleteCategoryHandler(context, Substitute.For<ICatalogMetrics>(), NullLogger<DeleteCategoryHandler>.Instance);

        var action = () => handler.Handle(new DeleteCategoryCommand(parent.Id), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Category cannot be deleted because it has child categories.");
    }
}
