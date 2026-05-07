namespace Inventory.API.Contracts.InventoryItems;

public sealed record AdjustStockRequest(
    int NewTotalQuantity,
    string Reason,
    string? ReferenceId);
