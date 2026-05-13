using System.Diagnostics.Metrics;
using Catalog.Application.Abstractions.Observability;

namespace Catalog.Infrastructure.Observability;

public sealed class CatalogMetrics : ICatalogMetrics
{
    public const string MeterName = "MarketplaceOrderPlatform.Catalog";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> ProductCreatedCounter =
        Meter.CreateCounter<long>("catalog.products.created");
    private static readonly Counter<long> ProductPriceUpdatedCounter =
        Meter.CreateCounter<long>("catalog.products.price_updated");
    private static readonly Counter<long> ProductUnavailableCounter =
        Meter.CreateCounter<long>("catalog.products.unavailable");
    private static readonly Counter<long> ProductVariantUnavailableCounter =
        Meter.CreateCounter<long>("catalog.product_variants.unavailable");
    private static readonly Counter<long> BrandCreatedCounter =
        Meter.CreateCounter<long>("catalog.brands.created");
    private static readonly Counter<long> BrandUpdatedCounter =
        Meter.CreateCounter<long>("catalog.brands.updated");
    private static readonly Counter<long> BrandDeletedCounter =
        Meter.CreateCounter<long>("catalog.brands.deleted");
    private static readonly Counter<long> CategoryCreatedCounter =
        Meter.CreateCounter<long>("catalog.categories.created");
    private static readonly Counter<long> CategoryUpdatedCounter =
        Meter.CreateCounter<long>("catalog.categories.updated");
    private static readonly Counter<long> CategoryDeletedCounter =
        Meter.CreateCounter<long>("catalog.categories.deleted");

    public void RecordProductCreated()
    {
        ProductCreatedCounter.Add(1);
    }

    public void RecordProductPriceUpdated()
    {
        ProductPriceUpdatedCounter.Add(1);
    }

    public void RecordProductUnavailable(string reason)
    {
        ProductUnavailableCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }

    public void RecordProductVariantUnavailable(string reason)
    {
        ProductVariantUnavailableCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }

    public void RecordBrandCreated()
    {
        BrandCreatedCounter.Add(1);
    }

    public void RecordBrandUpdated()
    {
        BrandUpdatedCounter.Add(1);
    }

    public void RecordBrandDeleted()
    {
        BrandDeletedCounter.Add(1);
    }

    public void RecordCategoryCreated()
    {
        CategoryCreatedCounter.Add(1);
    }

    public void RecordCategoryUpdated()
    {
        CategoryUpdatedCounter.Add(1);
    }

    public void RecordCategoryDeleted()
    {
        CategoryDeletedCounter.Add(1);
    }
}
