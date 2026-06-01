using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.ReleaseOrderStock;

public sealed class ReleaseOrderStockHandler(IInventoryDbContext context, IInventoryMetrics metrics)
    : IRequestHandler<ReleaseOrderStockCommand>
{
    public async Task Handle(ReleaseOrderStockCommand request, CancellationToken cancellationToken)
    {
        var inventoryItems = await LoadItemsAsync(request.Items, cancellationToken);
        var releasedCount = 0;

        foreach (var requestItem in request.Items)
        {
            var inventoryItem = FindInventoryItem(inventoryItems, requestItem.ProductId, requestItem.Sku);

            if (!inventoryItem.ReleaseReservation(request.OrderId, DateTime.UtcNow))
            {
                continue;
            }

            context.StockMovements.AddRange(inventoryItem.StockMovements);
            releasedCount++;
        }

        await SaveChangesAsync(cancellationToken);

        for (var index = 0; index < releasedCount; index++)
        {
            metrics.RecordReservationReleased();
        }
    }

    private async Task<List<InventoryItem>> LoadItemsAsync(
        IReadOnlyCollection<ReleaseOrderStockItem> requestedItems,
        CancellationToken cancellationToken)
    {
        var productIds = requestedItems.Select(item => item.ProductId).Distinct().ToList();

        return await context.InventoryItems
            .Include(item => item.Reservations)
            .Where(item => productIds.Contains(item.ProductId))
            .ToListAsync(cancellationToken);
    }

    private static InventoryItem FindInventoryItem(
        IReadOnlyCollection<InventoryItem> inventoryItems,
        Guid productId,
        string sku)
    {
        return inventoryItems.SingleOrDefault(item =>
                   item.ProductId == productId &&
                   string.Equals(item.Sku, sku.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? throw new NotFoundException(
                   $"Inventory item for product '{productId}' and SKU '{sku}' was not found.");
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Stock could not be released due to a concurrency conflict. {exception.Message}");
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Stock could not be released. {exception.Message}");
        }
    }
}
