using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Contracts.Payments;
using Payment.Application.Features.Payments.GetPaymentStatus;
using Payment.Application.Features.Payments.HandleProviderCallback;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var payment = await sender.Send(new GetPaymentStatusQuery(id), cancellationToken);
        return Ok(payment);
    }

    [HttpPost("{id:guid}/callback")]
    public async Task<IActionResult> Callback(
        Guid id,
        ProviderCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await sender.Send(
            new HandleProviderCallbackCommand(id, request.Approved, request.ProviderEventId),
            cancellationToken);

        return Ok(payment);
    }

    [HttpPost("webhooks/fake")]
    public async Task<IActionResult> FakeWebhook(ProviderCallbackRequest request, CancellationToken cancellationToken)
    {
        var payment = await sender.Send(
            new HandleProviderCallbackCommand(request.PaymentId, request.Approved, request.ProviderEventId),
            cancellationToken);

        return Ok(payment);
    }
}
