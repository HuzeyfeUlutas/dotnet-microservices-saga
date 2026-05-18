namespace Order.Application.Contracts.IntegrationEvents;

public sealed record OrderPaymentFailed(
    Guid EventId,
    Guid OrderId,
    Guid BuyerId,
    Guid PaymentId,
    decimal TotalAmount,
    string Currency,
    string FailureReason,
    IReadOnlyCollection<OrderLineSnapshotIntegrationItem> Items,
    DateTime OccurredAtUtc);
