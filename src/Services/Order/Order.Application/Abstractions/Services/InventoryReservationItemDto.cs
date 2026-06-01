namespace Order.Application.Abstractions.Services;

public sealed record InventoryReservationItemDto(
    Guid ProductId,
    string Sku,
    int Quantity);
