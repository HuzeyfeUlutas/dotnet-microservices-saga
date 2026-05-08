namespace Notification.Infrastructure.Observability;

public sealed class ElasticSearchOptions
{
    public const string SectionName = "ElasticSearch";

    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexFormat { get; set; } = "notification-logs-{0:yyyy.MM}";
}
