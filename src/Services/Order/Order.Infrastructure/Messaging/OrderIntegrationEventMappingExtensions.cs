using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Entities;

namespace Order.Infrastructure.Messaging;

internal static class OrderIntegrationEventMappingExtensions
{
    public static IReadOnlyCollection<OrderLineSnapshotIntegrationItem> ToIntegrationItems(this IEnumerable<OrderLine> lines)
    {
        return lines
            .OrderBy(line => line.Id)
            .Select(line => new OrderLineSnapshotIntegrationItem(
                line.Id,
                line.ProductId,
                line.Sku,
                line.ProductName,
                line.VariantName,
                line.UnitPrice,
                line.Currency,
                line.Quantity,
                line.LineTotal))
            .ToList();
    }
}
