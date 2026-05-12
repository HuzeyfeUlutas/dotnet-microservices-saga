using MassTransit;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions.Messaging;

namespace Payment.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(
    IPublishEndpoint publishEndpoint,
    ILogger<MassTransitIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        logger.LogInformation(
            "Publishing integration event {EventType}",
            typeof(TMessage).Name);

        return publishEndpoint.Publish(message, cancellationToken);
    }
}
