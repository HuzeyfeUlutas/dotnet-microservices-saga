using Order.Domain.Enums;

namespace Order.Application.DTOs;

public sealed record OrderDto(
    Guid OrderId,
    Guid BuyerId,
    OrderStatus Status,
    string Currency,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? FailureReason,
    IReadOnlyCollection<OrderLineDto> Items);
