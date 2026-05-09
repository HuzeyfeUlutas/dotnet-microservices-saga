using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Notification.Infrastructure.Observability;

public static class SerilogExtensions
{
    public static void ConfigureNotificationSerilog(
        this IHostBuilder hostBuilder,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", "Notification")
                .Enrich.WithProperty("Environment", environment.EnvironmentName)
                .WriteTo.Console();

            var elasticSearchOptions = BuildElasticSearchOptions(configuration);
            if (!string.IsNullOrWhiteSpace(elasticSearchOptions.Uri))
            {
                loggerConfiguration.WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri(elasticSearchOptions.Uri))
                    {
                        AutoRegisterTemplate = true,
                        IndexFormat = elasticSearchOptions.IndexFormat
                    });
            }
        });
    }

    private static ElasticSearchOptions BuildElasticSearchOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(ElasticSearchOptions.SectionName);

        return new ElasticSearchOptions
        {
            Uri = section["Uri"] ?? "http://localhost:9200",
            IndexFormat = section["IndexFormat"] ?? "notification-logs-{0:yyyy.MM}"
        };
    }
}
