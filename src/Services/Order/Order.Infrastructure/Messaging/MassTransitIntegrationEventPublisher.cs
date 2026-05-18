using MassTransit;
using Microsoft.Extensions.Logging;
using Order.Application.Abstractions.Messaging;
using Order.Application.Abstractions.Observability;

namespace Order.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(
    IPublishEndpoint publishEndpoint,
    ICorrelationContextAccessor correlationContextAccessor,
    ILogger<MassTransitIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        logger.LogInformation(
            "Publishing integration event {EventType}",
            typeof(TMessage).Name);

        return publishEndpoint.Publish(
            message,
            publishContext =>
            {
                if (Guid.TryParse(correlationContextAccessor.CorrelationId, out var correlationId))
                {
                    publishContext.CorrelationId = correlationId;
                }

                if (!string.IsNullOrWhiteSpace(correlationContextAccessor.CorrelationId))
                {
                    publishContext.Headers.Set("X-Correlation-Id", correlationContextAccessor.CorrelationId);
                }
            },
            cancellationToken);
    }
}
