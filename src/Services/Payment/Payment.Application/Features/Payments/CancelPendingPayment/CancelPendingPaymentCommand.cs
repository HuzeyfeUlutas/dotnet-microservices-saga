using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.CancelPendingPayment;

public sealed record CancelPendingPaymentCommand(
    Guid RequestEventId,
    Guid PaymentId,
    string IdempotencyKey,
    string Reason) : IRequest<PaymentDto>;
