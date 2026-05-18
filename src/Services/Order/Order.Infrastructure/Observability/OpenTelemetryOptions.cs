namespace Order.Infrastructure.Observability;

public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; init; } = "Order";
    public string ServiceVersion { get; init; } = "1.0.0";
    public string OtlpEndpoint { get; init; } = "http://localhost:4317";
}
