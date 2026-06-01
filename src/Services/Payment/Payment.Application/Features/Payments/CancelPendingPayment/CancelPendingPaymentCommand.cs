using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.CancelPendingPayment;

public sealed record CancelPendingPaymentCommand(Guid RequestEventId, Guid PaymentId, string Reason) : IRequest<PaymentDto>;
