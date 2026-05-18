using System.Net.Http.Headers;
using Order.Application.Abstractions.Observability;

namespace Order.Infrastructure.Services;

internal sealed class CorrelationPropagationHandler(ICorrelationContextAccessor correlationContextAccessor) : DelegatingHandler
{
    private const string CorrelationHeaderName = "X-Correlation-Id";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(correlationContextAccessor.CorrelationId) &&
            !request.Headers.Contains(CorrelationHeaderName))
        {
            request.Headers.Add(CorrelationHeaderName, correlationContextAccessor.CorrelationId);
        }

        if (request.Headers.Accept.Count == 0)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
