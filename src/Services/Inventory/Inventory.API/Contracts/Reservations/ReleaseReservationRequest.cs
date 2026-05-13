namespace Inventory.API.Contracts.Reservations;

public sealed record ReleaseReservationRequest(
    Guid ProductId,
    string Sku,
    Guid OrderId);
