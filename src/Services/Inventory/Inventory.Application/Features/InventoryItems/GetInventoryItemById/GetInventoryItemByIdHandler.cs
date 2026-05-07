using Inventory.Application.Abstractions.Persistence;
using Inventory.Application.Common.Exceptions;
using Inventory.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.InventoryItems.GetInventoryItemById;

public class GetInventoryItemByIdHandler(IInventoryDbContext context)
    : IRequestHandler<GetInventoryItemByIdQuery, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(GetInventoryItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems
            .AsNoTracking()
            .Where(x => x.Id == request.InventoryItemId)
            .Select(x => new InventoryItemDto(
                x.Id,
                x.ProductId,
                x.Sku,
                x.TotalQuantity,
                x.ReservedQuantity,
                x.AvailableQuantity,
                x.IsActive,
                x.Reservations
                    .OrderByDescending(reservation => reservation.ReservedAtUtc)
                    .Select(reservation => new InventoryReservationDto(
                        reservation.Id,
                        reservation.OrderId,
                        reservation.Quantity,
                        reservation.Status,
                        reservation.ReservedAtUtc,
                        reservation.ExpiresAtUtc,
                        reservation.ConfirmedAtUtc,
                        reservation.ReleasedAtUtc))
                    .ToList(),
                x.StockMovements
                    .OrderByDescending(movement => movement.OccurredAtUtc)
                    .Select(movement => new StockMovementDto(
                        movement.Id,
                        movement.Type,
                        movement.Quantity,
                        movement.Reason,
                        movement.ReferenceId,
                        movement.OccurredAtUtc))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return item ?? throw new NotFoundException($"Inventory item '{request.InventoryItemId}' was not found.");
    }
}
