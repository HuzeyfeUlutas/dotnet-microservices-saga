using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.InventoryItems.CreateInventoryItem;

public class CreateInventoryItemHandler(IInventoryDbContext context)
    : IRequestHandler<CreateInventoryItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var productExists = await context.InventoryItems
            .AnyAsync(x => x.ProductId == request.ProductId, cancellationToken);

        if (productExists)
        {
            throw new ConflictException($"Inventory item for product '{request.ProductId}' already exists.");
        }

        var skuExists = await context.InventoryItems
            .AnyAsync(x => x.Sku == request.Sku, cancellationToken);

        if (skuExists)
        {
            throw new ConflictException($"Inventory item with SKU '{request.Sku}' already exists.");
        }

        var item = new InventoryItem(request.ProductId, request.Sku, request.InitialQuantity);

        context.InventoryItems.Add(item);
        await SaveChangesAsync(cancellationToken);

        return item.Id;
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Inventory item could not be created due to a concurrency conflict. {exception.Message}");
        }
    }
}
