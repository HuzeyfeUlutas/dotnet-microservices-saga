using MediatR;

namespace Inventory.Application.Features.Reservations.CommitOrderStock;

public sealed record CommitOrderStockCommand(
    Guid OrderId,
    IReadOnlyCollection<CommitOrderStockItem> Items) : IRequest;

public sealed record CommitOrderStockItem(
    Guid ProductId,
    string Sku);
