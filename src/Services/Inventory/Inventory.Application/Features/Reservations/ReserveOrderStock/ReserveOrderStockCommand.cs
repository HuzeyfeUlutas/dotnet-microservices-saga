using MediatR;

namespace Inventory.Application.Features.Reservations.ReserveOrderStock;

public sealed record ReserveOrderStockCommand(
    Guid OrderId,
    IReadOnlyCollection<ReserveOrderStockItem> Items,
    DateTime? ExpiresAtUtc = null) : IRequest<IReadOnlyCollection<ReservedOrderStockItem>>;

public sealed record ReserveOrderStockItem(
    Guid ProductId,
    string Sku,
    int Quantity);

public sealed record ReservedOrderStockItem(
    Guid ReservationId,
    Guid ProductId,
    string Sku,
    Guid OrderId,
    int Quantity,
    string Status,
    DateTime? ExpiresAtUtc);
