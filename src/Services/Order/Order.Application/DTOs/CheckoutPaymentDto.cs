namespace Order.Application.DTOs;

public sealed record CheckoutPaymentDto(
    Guid PaymentId,
    string Status,
    string Provider,
    CheckoutPaymentActionDto Action);
