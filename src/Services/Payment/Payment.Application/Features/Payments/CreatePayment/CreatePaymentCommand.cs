using MediatR;
using Payment.Application.DTOs;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Payments.CreatePayment;

public sealed record CreatePaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    PaymentProviderType Provider = PaymentProviderType.Fake,
    PaymentMethodType Method = PaymentMethodType.Card) : IRequest<CreatePaymentResultDto>;
