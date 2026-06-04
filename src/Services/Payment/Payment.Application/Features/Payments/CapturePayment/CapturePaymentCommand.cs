using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.CapturePayment;

public sealed record CapturePaymentCommand(Guid PaymentId, string IdempotencyKey) : IRequest<PaymentDto>;
