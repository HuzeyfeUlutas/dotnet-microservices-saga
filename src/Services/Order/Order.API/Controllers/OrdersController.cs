using MediatR;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Features.Orders.GetOrderById;

namespace Order.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await sender.Send(new GetOrderByIdQuery(orderId), cancellationToken);
        return Ok(order);
    }
}
