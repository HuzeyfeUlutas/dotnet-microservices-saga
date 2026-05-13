using Inventory.Domain.Enums;

namespace Inventory.API.Contracts.Reservations;

public sealed record ReserveStockResponse(
    Guid ReservationId,
    Guid ProductId,
    string Sku,
    Guid OrderId,
    int Quantity,
    InventoryReservationStatus Status,
    DateTime? ExpiresAtUtc);
