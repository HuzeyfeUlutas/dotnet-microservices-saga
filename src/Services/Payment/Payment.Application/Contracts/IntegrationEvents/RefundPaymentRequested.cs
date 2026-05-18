namespace Payment.Application.Contracts.IntegrationEvents;

public sealed record RefundPaymentRequested(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    string? Reason,
    DateTime OccurredAtUtc);
