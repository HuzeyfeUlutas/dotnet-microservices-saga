namespace Payment.Application.DTOs;

public sealed record CreatePaymentResultDto(
    PaymentDto Payment,
    PaymentActionDto Action);
