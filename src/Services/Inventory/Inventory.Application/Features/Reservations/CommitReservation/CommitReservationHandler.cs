using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.CommitReservation;

public class CommitReservationHandler(IInventoryDbContext context, IInventoryMetrics metrics)
    : IRequestHandler<CommitReservationCommand>
{
    public async Task Handle(CommitReservationCommand request, CancellationToken cancellationToken)
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

        var wasCommitted = item.CommitReservation(request.OrderId, DateTime.UtcNow);
        await SaveChangesAsync(cancellationToken);

        if (wasCommitted)
        {
            metrics.RecordReservationCommitted();
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
            throw new ConflictException($"Reservation could not be committed due to a concurrency conflict. {exception.Message}");
        }
    }
}
