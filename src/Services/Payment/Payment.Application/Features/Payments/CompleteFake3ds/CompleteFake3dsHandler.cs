using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;

namespace Payment.Application.Features.Payments.CompleteFake3ds;

public class CompleteFake3dsHandler(
    IPaymentDbContext context,
    IPaymentProvider paymentProvider) : IRequestHandler<CompleteFake3dsCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(CompleteFake3dsCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException($"Payment '{request.PaymentId}' was not found.");
        }

        var providerResult = await paymentProvider.CompleteAuthorizationAsync(payment, request.Approved, cancellationToken);

        if (providerResult.Succeeded)
        {
            payment.MarkAsAuthorized(providerResult.ProviderPaymentId, providerResult.ProviderTransactionId);
        }
        else
        {
            payment.MarkAuthorizationAsFailed(providerResult.FailureReason ?? "Payment authorization failed.");
        }

        await context.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }
}
