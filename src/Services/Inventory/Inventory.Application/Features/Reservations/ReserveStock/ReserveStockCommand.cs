using MediatR;

namespace Inventory.Application.Features.Reservations.ReserveStock;

public sealed record ReserveStockCommand(
    Guid ProductId,
    Guid OrderId,
    int Quantity,
    DateTime? ExpiresAtUtc = null) : IRequest<Guid>;
