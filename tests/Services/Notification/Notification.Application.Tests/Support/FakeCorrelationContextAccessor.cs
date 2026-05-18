using Notification.Application.Abstractions.Observability;

namespace Notification.Application.Tests.Support;

internal sealed class FakeCorrelationContextAccessor : ICorrelationContextAccessor
{
    public string? CorrelationId { get; set; }
}
