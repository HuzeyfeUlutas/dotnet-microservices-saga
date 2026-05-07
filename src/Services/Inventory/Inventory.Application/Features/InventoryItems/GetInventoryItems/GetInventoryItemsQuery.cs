using Inventory.Application.DTOs;
using MediatR;

namespace Inventory.Application.Features.InventoryItems.GetInventoryItems;

public sealed record GetInventoryItemsQuery : IRequest<IReadOnlyCollection<InventoryItemListItemDto>>;
