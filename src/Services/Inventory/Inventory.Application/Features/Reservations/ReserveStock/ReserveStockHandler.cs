using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.ReserveStock;

public class ReserveStockHandler(IInventoryDbContext context, IInventoryMetrics metrics)
    : IRequestHandler<ReserveStockCommand, Guid>
{
    public async Task<Guid> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(
                x => x.ProductId == request.ProductId && x.Sku == request.Sku,
                cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(
                $"Inventory item for product '{request.ProductId}' and SKU '{request.Sku}' was not found.");
        }

        var reservation = ReserveItem(request, item);
        await SaveChangesAsync(cancellationToken);
        metrics.RecordStockReserved();

        return reservation.Id;
    }

    private InventoryReservation ReserveItem(
        ReserveStockCommand request,
        InventoryItem item)
    {
        try
        {
            return item.Reserve(request.OrderId, request.Quantity, DateTime.UtcNow, request.ExpiresAtUtc);
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
    }
}
