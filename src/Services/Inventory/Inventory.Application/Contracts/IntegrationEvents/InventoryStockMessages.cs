namespace Marketplace.Contracts.Inventory.V1;

public sealed record StockReservationItem(
    Guid ProductId,
    string Sku);

public sealed record CommitStockRequested(
    Guid EventId,
    Guid OrderId,
    IReadOnlyCollection<StockReservationItem> Items,
    DateTime OccurredAtUtc);

public sealed record StockCommitted(
    Guid EventId,
    Guid RequestEventId,
    Guid OrderId,
    DateTime OccurredAtUtc);

public sealed record StockCommitFailed(
    Guid EventId,
    Guid RequestEventId,
    Guid OrderId,
    string FailureReason,
    DateTime OccurredAtUtc);

public sealed record ReleaseStockRequested(
    Guid EventId,
    Guid OrderId,
    IReadOnlyCollection<StockReservationItem> Items,
    DateTime OccurredAtUtc);

public sealed record StockReleased(
    Guid EventId,
    Guid RequestEventId,
    Guid OrderId,
    DateTime OccurredAtUtc);

public sealed record StockReleaseFailed(
    Guid EventId,
    Guid RequestEventId,
    Guid OrderId,
    string FailureReason,
    DateTime OccurredAtUtc);

public sealed record ReverseCommittedStockRequested(
    Guid EventId,
    Guid OrderId,
    IReadOnlyCollection<StockReservationItem> Items,
    DateTime OccurredAtUtc);

public sealed record CommittedStockReversed(
    Guid EventId,
    Guid RequestEventId,
    Guid OrderId,
    DateTime OccurredAtUtc);

public sealed record CommittedStockReverseFailed(
    Guid EventId,
    Guid RequestEventId,
    Guid OrderId,
    string FailureReason,
    DateTime OccurredAtUtc);
