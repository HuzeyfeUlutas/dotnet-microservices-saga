namespace Payment.Application.Contracts.IntegrationEvents;

public sealed record PaymentAuthorizationFailed(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string FailureReason,
    DateTime OccurredAtUtc);
