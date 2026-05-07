using MediatR;

namespace Inventory.Application.Features.InventoryItems.AdjustStock;

public sealed record AdjustStockCommand(
    Guid InventoryItemId,
    int NewTotalQuantity,
    string Reason,
    string? ReferenceId = null) : IRequest;
