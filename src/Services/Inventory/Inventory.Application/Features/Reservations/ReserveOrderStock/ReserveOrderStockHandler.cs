using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.ReserveOrderStock;

public sealed class ReserveOrderStockHandler(IInventoryDbContext context, IInventoryMetrics metrics)
    : IRequestHandler<ReserveOrderStockCommand, IReadOnlyCollection<ReservedOrderStockItem>>
{
    public async Task<IReadOnlyCollection<ReservedOrderStockItem>> Handle(
        ReserveOrderStockCommand request,
        CancellationToken cancellationToken)
    {
        var requestedProductIds = request.Items
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        var inventoryItems = await context.InventoryItems
            .Include(item => item.Reservations)
            .Where(item => requestedProductIds.Contains(item.ProductId))
            .ToListAsync(cancellationToken);

        var reservedAtUtc = DateTime.UtcNow;
        var reservedItems = new List<ReservedOrderStockItem>(request.Items.Count);

        foreach (var requestItem in request.Items)
        {
            var inventoryItem = FindInventoryItem(inventoryItems, requestItem);
            var existingReservationIds = inventoryItem.Reservations
                .Select(reservation => reservation.Id)
                .ToHashSet();
            var existingStockMovementIds = inventoryItem.StockMovements
                .Select(movement => movement.Id)
                .ToHashSet();
            var reservation = ReserveItem(inventoryItem, request, requestItem, reservedAtUtc);

            if (!existingReservationIds.Contains(reservation.Id))
            {
                context.InventoryReservations.Add(reservation);
            }

            context.StockMovements.AddRange(
                inventoryItem.StockMovements.Where(movement => !existingStockMovementIds.Contains(movement.Id)));

            reservedItems.Add(new ReservedOrderStockItem(
                reservation.Id,
                inventoryItem.ProductId,
                inventoryItem.Sku,
                reservation.OrderId,
                reservation.Quantity,
                reservation.Status.ToString(),
                reservation.ExpiresAtUtc));
        }

        await SaveChangesAsync(cancellationToken);

        foreach (var _ in reservedItems)
        {
            metrics.RecordStockReserved();
        }

        return reservedItems;
    }

    private static InventoryItem FindInventoryItem(
        IReadOnlyCollection<InventoryItem> inventoryItems,
        ReserveOrderStockItem requestItem)
    {
        var inventoryItem = inventoryItems.SingleOrDefault(item =>
            item.ProductId == requestItem.ProductId &&
            string.Equals(item.Sku, requestItem.Sku.Trim(), StringComparison.OrdinalIgnoreCase));

        return inventoryItem ?? throw new NotFoundException(
            $"Inventory item for product '{requestItem.ProductId}' and SKU '{requestItem.Sku}' was not found.");
    }

    private InventoryReservation ReserveItem(
        InventoryItem inventoryItem,
        ReserveOrderStockCommand request,
        ReserveOrderStockItem requestItem,
        DateTime reservedAtUtc)
    {
        try
        {
            return inventoryItem.Reserve(
                request.OrderId,
                requestItem.Quantity,
                reservedAtUtc,
                request.ExpiresAtUtc);
        }
        catch (DomainException exception)
        {
            metrics.RecordStockUnavailable(exception.Message);
            throw;
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Stock could not be reserved due to a concurrency conflict. {exception.Message}");
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Stock could not be reserved. {exception.Message}");
        }
    }
}
