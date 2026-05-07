using Inventory.Application.Abstractions.Observability;

namespace Inventory.API.Observability;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    public string? CorrelationId { get; set; }
}
