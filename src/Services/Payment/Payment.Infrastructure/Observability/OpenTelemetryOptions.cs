namespace Payment.Infrastructure.Observability;

public sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; init; } = "Payment";
    public string ServiceVersion { get; init; } = "1.0.0";
    public string OtlpEndpoint { get; init; } = "http://localhost:4317";
}
