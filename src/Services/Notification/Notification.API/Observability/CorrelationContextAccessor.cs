using Notification.Application.Abstractions.Observability;

namespace Notification.API.Observability;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    public string? CorrelationId { get; set; }
}
