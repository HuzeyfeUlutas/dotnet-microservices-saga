namespace Order.Application.Abstractions.Services;

public interface IInventoryReservationClient
{
    Task<InventoryReservationResultDto> ReserveAsync(
        Guid productId,
        string sku,
        Guid orderId,
        int quantity,
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
