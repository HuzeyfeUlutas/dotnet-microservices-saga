using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.RefundPayment;

public sealed record RefundPaymentCommand(Guid PaymentId) : IRequest<PaymentDto>;
