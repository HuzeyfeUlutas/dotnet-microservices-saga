using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions.Persistence;
using Order.Application.Common.Exceptions;
using Order.Application.DTOs;

namespace Order.Application.Features.Orders.GetOrderById;

public class GetOrderByIdHandler(IOrderDbContext context) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Where(x => x.Id == request.OrderId)
            .Select(x => new OrderDto(
                x.Id,
                x.BuyerId,
                x.Status,
                x.Currency,
                x.TotalAmount,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.FailureReason,
                x.Lines
                    .OrderBy(line => line.Id)
                    .Select(line => new OrderLineDto(
                        line.Id,
                        line.ProductId,
                        line.Sku,
                        line.ProductName,
                        line.VariantName,
                        line.UnitPrice,
                        line.Currency,
                        line.Quantity,
                        line.LineTotal))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order '{request.OrderId}' was not found.");
        }

        return order;
    }
}
