using Payment.Application.DTOs;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments;

internal static class PaymentMappingExtensions
{
    public static PaymentDto ToDto(this PaymentEntity payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.Amount.Amount,
            payment.Amount.Currency,
            payment.Provider,
            payment.Method,
            payment.Status,
            payment.IdempotencyKey,
            payment.CreatedAtUtc,
            payment.AuthorizedAtUtc,
            payment.CapturedAtUtc,
            payment.RefundedAtUtc,
            payment.FailureReason);
    }
}
