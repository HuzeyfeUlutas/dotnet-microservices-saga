using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.InventoryItems.GetInventoryItems;

public class GetInventoryItemsHandler(IInventoryDbContext context)
    : IRequestHandler<GetInventoryItemsQuery, IReadOnlyCollection<InventoryItemListItemDto>>
{
    public async Task<IReadOnlyCollection<InventoryItemListItemDto>> Handle(
        GetInventoryItemsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await context.InventoryItems
            .AsNoTracking()
            .OrderBy(x => x.Sku)
            .Select(x => new InventoryItemListItemDto(
                x.Id,
                x.ProductId,
                x.Sku,
                x.TotalQuantity,
                x.ReservedQuantity,
                x.AvailableQuantity,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return items;
    }
}
