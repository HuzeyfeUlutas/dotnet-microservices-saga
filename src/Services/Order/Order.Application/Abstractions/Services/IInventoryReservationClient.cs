namespace Order.Application.Abstractions.Services;

public interface IInventoryReservationClient
{
    Task<IReadOnlyCollection<InventoryReservationResultDto>> ReserveOrderStockAsync(
        Guid orderId,
        IReadOnlyCollection<InventoryReservationItemDto> items,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task CommitAsync(
        Guid productId,
        string sku,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        Guid productId,
        string sku,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
