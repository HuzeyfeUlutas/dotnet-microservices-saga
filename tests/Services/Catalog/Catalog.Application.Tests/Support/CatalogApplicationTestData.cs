using System.Threading;
using System.Threading.Tasks;
using Catalog.Domain.Entities;
using Catalog.Persistence.Context;

namespace Catalog.Application.Tests.Support;

internal static class CatalogApplicationTestData
{
    public static async Task<(Brand Brand, Category Category)> SeedBrandAndCategoryAsync(
        CatalogDbContext context,
        CancellationToken cancellationToken = default)
    {
        var brand = new Brand("Apple");
        var category = new Category("Phones");

        context.Brands.Add(brand);
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        return (brand, category);
    }
}
