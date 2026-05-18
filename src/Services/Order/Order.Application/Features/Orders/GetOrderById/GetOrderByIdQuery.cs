using MediatR;
using Order.Application.DTOs;

namespace Order.Application.Features.Orders.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;
