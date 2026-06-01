using MediatR;

namespace Inventory.Application.Features.Reservations.ReverseCommittedOrderStock;

public sealed record ReverseCommittedOrderStockCommand(
    Guid OrderId,
    IReadOnlyCollection<ReverseCommittedOrderStockItem> Items) : IRequest;

public sealed record ReverseCommittedOrderStockItem(
    Guid ProductId,
    string Sku);
