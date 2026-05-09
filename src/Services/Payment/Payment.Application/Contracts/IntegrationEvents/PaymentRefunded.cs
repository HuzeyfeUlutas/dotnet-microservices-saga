namespace Payment.Application.Contracts.IntegrationEvents;

public sealed record PaymentRefunded(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc);
