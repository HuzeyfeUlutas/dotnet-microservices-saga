using Marketplace.Contracts.Inventory.V1;

namespace Order.Application.Abstractions.Messaging;

public interface IStockReservationRollbackPublisher
{
    Task PublishReleaseImmediatelyAsync(
        Guid orderId,
        IReadOnlyCollection<StockReservationItem> items,
        CancellationToken cancellationToken = default);
}
