namespace Payment.Infrastructure.Observability;

public sealed class ElasticSearchOptions
{
    public const string SectionName = "ElasticSearch";

    public string? Uri { get; init; }
    public string? IndexFormat { get; init; }
}
