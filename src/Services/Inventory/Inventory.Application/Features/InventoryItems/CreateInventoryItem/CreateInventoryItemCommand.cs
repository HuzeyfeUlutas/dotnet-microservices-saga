using MediatR;

namespace Inventory.Application.Features.InventoryItems.CreateInventoryItem;

public sealed record CreateInventoryItemCommand(
    Guid ProductId,
    string Sku,
    int InitialQuantity) : IRequest<Guid>;
