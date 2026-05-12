using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.CompleteFake3ds;

public sealed record CompleteFake3dsCommand(Guid PaymentId, bool Approved) : IRequest<PaymentDto>;
