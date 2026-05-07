using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.ReleaseReservation;

public class ReleaseReservationHandler(IInventoryDbContext context)
    : IRequestHandler<ReleaseReservationCommand>
{
    public async Task Handle(ReleaseReservationCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException($"Inventory item for product '{request.ProductId}' was not found.");
        }

        item.ReleaseReservation(request.OrderId, DateTime.UtcNow);
        await SaveChangesAsync(cancellationToken);
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
