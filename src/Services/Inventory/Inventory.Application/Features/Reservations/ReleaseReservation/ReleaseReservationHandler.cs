using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.ReleaseReservation;

public class ReleaseReservationHandler(IInventoryDbContext context, IInventoryMetrics metrics)
    : IRequestHandler<ReleaseReservationCommand>
{
    public async Task Handle(ReleaseReservationCommand request, CancellationToken cancellationToken)
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

        item.ReleaseReservation(request.OrderId, DateTime.UtcNow);
        await SaveChangesAsync(cancellationToken);
        metrics.RecordReservationReleased();
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Reservation could not be released due to a concurrency conflict. {exception.Message}");
        }
    }
}
