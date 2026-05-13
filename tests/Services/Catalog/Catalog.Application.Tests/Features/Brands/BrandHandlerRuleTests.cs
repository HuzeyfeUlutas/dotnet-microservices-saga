using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions.Observability;
using Catalog.Application.Common.Exceptions;
using Catalog.Application.Features.Brands.CreateBrand;
using Catalog.Application.Features.Brands.DeleteBrand;
using Catalog.Application.Features.Brands.UpdateBrand;
using Catalog.Application.Tests.Support;
using Catalog.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Catalog.Application.Tests.Features.Brands;

public class BrandHandlerRuleTests
{
    [Fact]
    public async Task CreateBrand_should_throw_conflict_when_name_already_exists()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        context.Brands.Add(new Brand("Apple"));
        await context.SaveChangesAsync();
        var handler = new CreateBrandHandler(context, Substitute.For<ICatalogMetrics>(), NullLogger<CreateBrandHandler>.Instance);

        var action = () => handler.Handle(new CreateBrandCommand(" apple ", null), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Brand name 'apple' already exists.");
    }

    [Fact]
    public async Task UpdateBrand_should_throw_conflict_when_name_belongs_to_another_brand()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var apple = new Brand("Apple");
        var samsung = new Brand("Samsung");
        context.Brands.AddRange(apple, samsung);
        await context.SaveChangesAsync();
        var handler = new UpdateBrandHandler(context, Substitute.For<ICatalogMetrics>(), NullLogger<UpdateBrandHandler>.Instance);

        var action = () => handler.Handle(new UpdateBrandCommand(samsung.Id, " apple ", null, true), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Brand name 'apple' already exists.");
    }

    [Fact]
    public async Task DeleteBrand_should_throw_conflict_when_brand_has_products()
    {
        using var factory = new CatalogTestDbContextFactory();
        await using var context = factory.CreateContext();
        var (brand, category) = await CatalogApplicationTestData.SeedBrandAndCategoryAsync(context);
        context.Products.Add(new Product("iPhone 15", "Smartphone", 49999.99m, brand.Id, category.Id));
        await context.SaveChangesAsync();
        var handler = new DeleteBrandHandler(context, Substitute.For<ICatalogMetrics>(), NullLogger<DeleteBrandHandler>.Instance);

        var action = () => handler.Handle(new DeleteBrandCommand(brand.Id), CancellationToken.None);

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Brand cannot be deleted because it has products.");
    }
}
