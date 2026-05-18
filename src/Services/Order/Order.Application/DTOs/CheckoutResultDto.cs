using Order.Domain.Enums;

namespace Order.Application.DTOs;

public sealed record CheckoutResultDto(
    Guid OrderId,
    OrderStatus OrderStatus,
    CheckoutPaymentDto Payment);
