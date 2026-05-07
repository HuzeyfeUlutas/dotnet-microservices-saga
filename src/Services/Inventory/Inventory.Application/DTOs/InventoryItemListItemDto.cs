namespace Inventory.Application.DTOs;

public sealed record InventoryItemListItemDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    int TotalQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    bool IsActive);
