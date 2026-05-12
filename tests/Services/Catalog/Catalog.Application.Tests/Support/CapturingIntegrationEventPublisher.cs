using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catalog.Application.Abstractions.Messaging;

namespace Catalog.Application.Tests.Support;

internal sealed class CapturingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly List<object> _publishedMessages = [];

    public IReadOnlyCollection<object> PublishedMessages => _publishedMessages.AsReadOnly();

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        _publishedMessages.Add(message);

        return Task.CompletedTask;
    }
}
