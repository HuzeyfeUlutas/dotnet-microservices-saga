using Inventory.API.Contracts.Reservations;
using Inventory.Application.Features.Reservations.CommitReservation;
using Inventory.Application.Features.Reservations.ReleaseReservation;
using Inventory.Application.Features.Reservations.ReserveStock;
using Inventory.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Reserve(
        [FromBody] ReserveStockRequest request,
        CancellationToken cancellationToken)
    {
        var reservationId = await sender.Send(
            new ReserveStockCommand(
                request.ProductId,
                request.Sku,
                request.OrderId,
                request.Quantity,
                request.ExpiresAtUtc),
            cancellationToken);

        var response = new ReserveStockResponse(
            reservationId,
            request.ProductId,
            request.Sku,
            request.OrderId,
            request.Quantity,
            InventoryReservationStatus.Pending,
            request.ExpiresAtUtc);

        return Ok(response);
    }

    [HttpPost("commit")]
    public async Task<IActionResult> Commit(
        [FromBody] CommitReservationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new CommitReservationCommand(request.ProductId, request.Sku, request.OrderId),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release(
        [FromBody] ReleaseReservationRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ReleaseReservationCommand(request.ProductId, request.Sku, request.OrderId),
            cancellationToken);

        return NoContent();
    }
}
