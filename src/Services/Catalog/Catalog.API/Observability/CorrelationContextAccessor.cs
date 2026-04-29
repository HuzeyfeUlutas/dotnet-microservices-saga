using Catalog.Application.Abstractions.Observability;

namespace Catalog.API.Observability;

public sealed class CorrelationContextAccessor : ICorrelationContextAccessor
{
    public string? CorrelationId { get; set; }
}
