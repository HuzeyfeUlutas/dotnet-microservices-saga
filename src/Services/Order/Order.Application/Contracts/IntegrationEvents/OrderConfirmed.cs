namespace Order.Application.Contracts.IntegrationEvents;

public sealed record OrderConfirmed(
    Guid EventId,
    Guid OrderId,
    Guid BuyerId,
    Guid PaymentId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyCollection<OrderLineSnapshotIntegrationItem> Items,
    DateTime OccurredAtUtc);
