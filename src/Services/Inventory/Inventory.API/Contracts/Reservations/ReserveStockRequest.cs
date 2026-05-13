namespace Inventory.API.Contracts.Reservations;

public sealed record ReserveStockRequest(
    Guid ProductId,
    string Sku,
    Guid OrderId,
    int Quantity,
    DateTime? ExpiresAtUtc);
