using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Abstractions.Observability;
using Inventory.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.InventoryItems.AdjustStock;

public class AdjustStockHandler(IInventoryDbContext context, IInventoryMetrics metrics)
    : IRequestHandler<AdjustStockCommand>
{
    public async Task Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems
            .FirstOrDefaultAsync(x => x.Id == request.InventoryItemId, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException($"Inventory item '{request.InventoryItemId}' was not found.");
        }

        item.AdjustStock(request.NewTotalQuantity, request.Reason, request.ReferenceId);
        await SaveChangesAsync(cancellationToken);
        metrics.RecordStockAdjusted();
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Inventory item could not be adjusted due to a concurrency conflict. {exception.Message}");
        }
    }
}
