using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.API.Contracts.Checkout;
using Order.Application.Features.Checkout.CreateCheckout;

namespace Order.API.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutController(ISender sender) : ControllerBase
{
    [HttpPost("pay")]
    public async Task<IActionResult> Pay(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCheckoutCommand(
                request.BuyerId,
                request.Items
                    .Select(item => new CreateCheckoutItem(item.ProductId, item.Sku, item.Quantity))
                    .ToList(),
                request.IdempotencyKey,
                request.Provider,
                request.Method),
            cancellationToken);

        return CreatedAtAction(nameof(OrdersController.GetById), "Orders", new { orderId = result.OrderId }, result);
    }
}
