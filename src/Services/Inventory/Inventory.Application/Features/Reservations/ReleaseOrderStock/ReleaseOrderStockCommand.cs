using MediatR;

namespace Inventory.Application.Features.Reservations.ReleaseOrderStock;

public sealed record ReleaseOrderStockCommand(
    Guid OrderId,
    IReadOnlyCollection<ReleaseOrderStockItem> Items) : IRequest;

public sealed record ReleaseOrderStockItem(
    Guid ProductId,
    string Sku);
