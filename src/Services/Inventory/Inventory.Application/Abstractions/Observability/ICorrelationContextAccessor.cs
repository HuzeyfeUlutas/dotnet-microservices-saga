namespace Inventory.Application.Abstractions.Observability;

public interface ICorrelationContextAccessor
{
    string? CorrelationId { get; set; }
}
