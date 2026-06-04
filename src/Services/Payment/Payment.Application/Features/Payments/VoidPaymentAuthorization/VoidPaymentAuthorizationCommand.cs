using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.VoidPaymentAuthorization;

public sealed record VoidPaymentAuthorizationCommand(
    Guid RequestEventId,
    Guid PaymentId,
    string IdempotencyKey) : IRequest<PaymentDto>;
