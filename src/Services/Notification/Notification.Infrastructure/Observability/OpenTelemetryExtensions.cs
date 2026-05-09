using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Notification.Infrastructure.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddNotificationTracing(this IServiceCollection services, IConfiguration configuration)
    {
        var options = BuildOptions(configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion,
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("MassTransit")
                .AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(options.OtlpEndpoint);
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(NotificationMetrics.MeterName)
                .AddMeter("MassTransit")
                .AddPrometheusExporter());

        return services;
    }

    private static OpenTelemetryOptions BuildOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(OpenTelemetryOptions.SectionName);

        return new OpenTelemetryOptions
        {
            ServiceName = section["ServiceName"] ?? "Notification",
            ServiceVersion = section["ServiceVersion"] ?? "1.0.0",
            OtlpEndpoint = section["OtlpEndpoint"] ?? "http://localhost:4317"
        };
    }
}
