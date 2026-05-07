using Inventory.Application.DTOs;
using MediatR;

namespace Inventory.Application.Features.InventoryItems.GetInventoryItemByProductId;

public sealed record GetInventoryItemByProductIdQuery(Guid ProductId) : IRequest<InventoryItemDto>;
