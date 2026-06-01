using Marketplace.Contracts.Inventory.V1;
using Order.Domain.Entities;

namespace Order.Infrastructure.Messaging;

internal static class StockReservationItemMappingExtensions
{
    public static IReadOnlyCollection<StockReservationItem> ToStockReservationItems(
        this IEnumerable<OrderLine> lines)
    {
        return lines
            .Select(line => new StockReservationItem(line.ProductId, line.Sku))
            .ToList();
    }
}
