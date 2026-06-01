namespace Order.Application.Abstractions.Services;

public interface IInventoryReservationClient
{
    Task<IReadOnlyCollection<InventoryReservationResultDto>> ReserveOrderStockAsync(
        Guid orderId,
        IReadOnlyCollection<InventoryReservationItemDto> items,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default);

}
