namespace Inventory.API.Contracts.InventoryItems;

public sealed record IncreaseStockRequest(
    int Quantity,
    string Reason,
    string? ReferenceId);
