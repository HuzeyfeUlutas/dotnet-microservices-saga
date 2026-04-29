namespace Catalog.Infrastructure.Observability;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = "Catalog";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
}
