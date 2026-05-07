using Inventory.Application.DTOs;
using MediatR;

namespace Inventory.Application.Features.InventoryItems.GetInventoryItemById;

public sealed record GetInventoryItemByIdQuery(Guid InventoryItemId) : IRequest<InventoryItemDto>;
