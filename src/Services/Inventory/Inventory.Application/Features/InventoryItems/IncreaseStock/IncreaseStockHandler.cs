using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.InventoryItems.IncreaseStock;

public class IncreaseStockHandler(IInventoryDbContext context, IInventoryMetrics metrics)
    : IRequestHandler<IncreaseStockCommand>
{
    public async Task Handle(IncreaseStockCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems
            .FirstOrDefaultAsync(x => x.Id == request.InventoryItemId, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException($"Inventory item '{request.InventoryItemId}' was not found.");
        }

        item.IncreaseStock(request.Quantity, request.Reason, request.ReferenceId);
        await SaveChangesAsync(cancellationToken);
        metrics.RecordStockIncreased();
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Inventory item could not be updated due to a concurrency conflict. {exception.Message}");
        }
    }
}
