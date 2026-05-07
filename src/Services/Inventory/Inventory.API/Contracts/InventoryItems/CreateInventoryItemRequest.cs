namespace Inventory.API.Contracts.InventoryItems;

public sealed record CreateInventoryItemRequest(
    Guid ProductId,
    string Sku,
    int InitialQuantity);
