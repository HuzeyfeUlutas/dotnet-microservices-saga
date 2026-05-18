using Order.Application.Abstractions.Observability;

namespace Order.API.Observability;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    public string? CorrelationId { get; set; }
}
