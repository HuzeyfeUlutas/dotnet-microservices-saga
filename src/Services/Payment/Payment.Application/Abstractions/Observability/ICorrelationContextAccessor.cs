namespace Payment.Application.Abstractions.Observability;

public interface ICorrelationContextAccessor
{
    string? CorrelationId { get; set; }
}
