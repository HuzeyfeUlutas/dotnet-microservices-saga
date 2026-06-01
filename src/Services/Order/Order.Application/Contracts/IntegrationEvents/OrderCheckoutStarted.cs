namespace Order.Application.Contracts.IntegrationEvents;

public sealed record OrderCheckoutStarted(
    Guid EventId,
    Guid OrderId,
    Guid PaymentId,
    DateTime OccurredAtUtc);
