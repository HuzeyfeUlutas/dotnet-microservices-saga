namespace Payment.Application.Contracts.IntegrationEvents;

public sealed record CapturePaymentRequested(
    Guid EventId,
    Guid PaymentId,
    Guid OrderId,
    DateTime OccurredAtUtc);
