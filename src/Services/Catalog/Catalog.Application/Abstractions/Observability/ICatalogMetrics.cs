namespace Catalog.Application.Abstractions.Observability;

public interface ICatalogMetrics
{
    void RecordProductCreated();
    void RecordProductPriceUpdated();
    void RecordProductUnavailable(string reason);
    void RecordProductVariantUnavailable(string reason);
    void RecordBrandCreated();
    void RecordBrandUpdated();
    void RecordBrandDeleted();
    void RecordCategoryCreated();
    void RecordCategoryUpdated();
    void RecordCategoryDeleted();
}
