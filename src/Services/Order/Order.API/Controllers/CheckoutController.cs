using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.API.Contracts.Checkout;
using Order.API.Observability;
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
        if (!TryResolveBuyerIdFromGatewayContext(out var buyerId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authenticated user context is missing.",
                detail: $"The checkout endpoint requires a valid {GatewayIdentityHeaderNames.UserId} header from the API Gateway.");
        }

        if (request.BuyerId is { } bodyBuyerId && bodyBuyerId != buyerId)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Buyer identity mismatch.",
                detail: "The checkout buyer identity must match the authenticated user.");
        }

        var items = request.Items?
            .Select(item => new CreateCheckoutItem(item.ProductId, item.Sku, item.Quantity))
            .ToList() ?? [];

        var result = await sender.Send(
            new CreateCheckoutCommand(
                buyerId,
                items,
                request.IdempotencyKey,
                request.Provider,
                request.Method),
            cancellationToken);

        return CreatedAtAction(nameof(OrdersController.GetById), "Orders", new { orderId = result.OrderId }, result);
    }

    private bool TryResolveBuyerIdFromGatewayContext(out Guid buyerId)
    {
        buyerId = default;

        if (!Request.Headers.TryGetValue(GatewayIdentityHeaderNames.UserId, out var values))
        {
            return false;
        }

        var value = values.FirstOrDefault();

        return Guid.TryParse(value, out buyerId) && buyerId != Guid.Empty;
    }
}
