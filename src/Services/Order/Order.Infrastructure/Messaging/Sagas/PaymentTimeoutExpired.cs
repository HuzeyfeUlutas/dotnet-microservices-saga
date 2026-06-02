namespace Order.Infrastructure.Messaging.Sagas;

public sealed record PaymentTimeoutExpired(
    Guid EventId,
    Guid OrderId,
    DateTime OccurredAtUtc);
