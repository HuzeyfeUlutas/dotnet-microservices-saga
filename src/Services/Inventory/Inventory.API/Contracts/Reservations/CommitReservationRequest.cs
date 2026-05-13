namespace Inventory.API.Contracts.Reservations;

public sealed record CommitReservationRequest(
    Guid ProductId,
    string Sku,
    Guid OrderId);
