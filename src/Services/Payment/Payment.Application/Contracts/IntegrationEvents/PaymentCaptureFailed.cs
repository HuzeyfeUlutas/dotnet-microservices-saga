namespace Payment.Application.Contracts.IntegrationEvents;

public sealed record PaymentCaptureFailed(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string FailureReason,
    DateTime OccurredAtUtc);
