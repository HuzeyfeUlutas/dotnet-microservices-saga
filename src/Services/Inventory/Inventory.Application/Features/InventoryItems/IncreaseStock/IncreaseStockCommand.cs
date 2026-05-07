using MediatR;

namespace Inventory.Application.Features.InventoryItems.IncreaseStock;

public sealed record IncreaseStockCommand(
    Guid InventoryItemId,
    int Quantity,
    string Reason,
    string? ReferenceId = null) : IRequest;
