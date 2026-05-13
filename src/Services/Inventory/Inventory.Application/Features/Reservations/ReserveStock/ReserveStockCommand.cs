using MediatR;

namespace Inventory.Application.Features.Reservations.ReserveStock;

public sealed record ReserveStockCommand(
    Guid ProductId,
    string Sku,
    Guid OrderId,
    int Quantity,
    DateTime? ExpiresAtUtc = null) : IRequest<Guid>;
