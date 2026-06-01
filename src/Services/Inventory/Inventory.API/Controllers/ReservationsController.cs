using Inventory.API.Contracts.Reservations;
using Inventory.Application.Features.Reservations.CommitReservation;
using Inventory.Application.Features.Reservations.ReleaseReservation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController(ISender sender) : ControllerBase
{
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
