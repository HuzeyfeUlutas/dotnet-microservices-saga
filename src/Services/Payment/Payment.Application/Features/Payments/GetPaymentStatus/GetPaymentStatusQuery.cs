using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.GetPaymentStatus;

public sealed record GetPaymentStatusQuery(Guid PaymentId) : IRequest<PaymentDto>;
