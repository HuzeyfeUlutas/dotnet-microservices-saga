namespace Order.Infrastructure.Observability;

public class ElasticSearchOptions
{
    public const string SectionName = "ElasticSearch";

    public string Uri { get; init; } = "http://localhost:9200";
    public string IndexFormat { get; init; } = "order-logs-{0:yyyy.MM}";
}
