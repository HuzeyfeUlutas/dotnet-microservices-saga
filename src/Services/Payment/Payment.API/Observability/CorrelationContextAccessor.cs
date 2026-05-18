using Payment.Application.Abstractions.Observability;

namespace Payment.API.Observability;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    public string? CorrelationId { get; set; }
}
