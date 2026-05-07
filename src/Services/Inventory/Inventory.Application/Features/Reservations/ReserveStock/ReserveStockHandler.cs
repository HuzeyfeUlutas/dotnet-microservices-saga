using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.ReserveStock;

public class ReserveStockHandler(IInventoryDbContext context)
    : IRequestHandler<ReserveStockCommand, Guid>
{
    public async Task<Guid> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems
            .Include(x => x.Reservations)
            .FirstOrDefaultAsync(x => x.ProductId == request.ProductId, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException($"Inventory item for product '{request.ProductId}' was not found.");
        }

        var reservation = item.Reserve(request.OrderId, request.Quantity, DateTime.UtcNow, request.ExpiresAtUtc);
        await SaveChangesAsync(cancellationToken);

        return reservation.Id;
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
