namespace Order.Application.Abstractions.Services;

public sealed record InventoryReservationResultDto(
    Guid ReservationId,
    Guid ProductId,
    string Sku,
    Guid OrderId,
    int Quantity,
    string Status,
    DateTime? ExpiresAtUtc);
