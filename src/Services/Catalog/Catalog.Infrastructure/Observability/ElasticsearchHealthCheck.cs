using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Catalog.Infrastructure.Observability;

internal sealed class ElasticsearchHealthCheck(IHttpClientFactory httpClientFactory, ElasticSearchOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Uri))
        {
            return HealthCheckResult.Healthy("Elasticsearch health check is disabled.");
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient(HealthCheckExtensions.ElasticsearchHealthClientName);
            using var response = await httpClient.GetAsync(options.Uri, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Elasticsearch is reachable.")
                : HealthCheckResult.Unhealthy(
                    $"Elasticsearch returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Elasticsearch is unreachable.", exception);
        }
    }
}
