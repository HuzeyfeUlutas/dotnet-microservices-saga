namespace Catalog.Infrastructure.Observability;

public sealed class ElasticSearchOptions
{
    public const string SectionName = "ElasticSearch";

    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexFormat { get; set; } = "catalog-logs-{0:yyyy.MM}";
}
