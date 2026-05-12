using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.GetPaymentStatus;

public class GetPaymentStatusHandler(IPaymentDbContext context) : IRequestHandler<GetPaymentStatusQuery, PaymentDto>
{
    public async Task<PaymentDto> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .AsNoTracking()
            .Where(x => x.Id == request.PaymentId)
            .Select(x => new PaymentDto(
                x.Id,
                x.OrderId,
                x.Amount.Amount,
                x.Amount.Currency,
                x.Provider,
                x.Method,
                x.Status,
                x.IdempotencyKey,
                x.CreatedAtUtc,
                x.AuthorizedAtUtc,
                x.CapturedAtUtc,
                x.RefundedAtUtc,
                x.FailureReason))
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException($"Payment '{request.PaymentId}' was not found.");
        }

        return payment;
    }
}
