namespace Inventory.Application.Abstractions.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
