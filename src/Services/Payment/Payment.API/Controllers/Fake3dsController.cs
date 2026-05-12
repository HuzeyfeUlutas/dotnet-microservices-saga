using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Contracts.Payments;
using Payment.Application.Features.Payments.CompleteFake3ds;

namespace Payment.API.Controllers;

[ApiController]
[Route("fake-3ds/payments")]
public class Fake3dsController(ISender sender) : ControllerBase
{
    [HttpGet("{paymentId:guid}")]
    public IActionResult Get(Guid paymentId)
    {
        return Ok(new
        {
            paymentId,
            completeUrl = $"/fake-3ds/payments/{paymentId}/complete"
        });
    }

    [HttpPost("{paymentId:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid paymentId,
        CompleteFake3dsRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await sender.Send(
            new CompleteFake3dsCommand(paymentId, request.Approved),
            cancellationToken);

        return Ok(payment);
    }
}
