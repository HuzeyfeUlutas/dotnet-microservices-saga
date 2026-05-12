using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.Contracts.IntegrationEvents;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.RefundPayment;

public class RefundPaymentHandler(
    IPaymentDbContext context,
    IPaymentProvider paymentProvider,
    IIntegrationEventPublisher integrationEventPublisher) : IRequestHandler<RefundPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException($"Payment '{request.PaymentId}' was not found.");
        }

        payment.StartRefund();
        var providerResult = await paymentProvider.RefundAsync(payment, cancellationToken);

        if (providerResult.Succeeded)
        {
            payment.MarkAsRefunded(providerResult.ProviderTransactionId);
        }
        else
        {
            payment.MarkRefundAsFailed(providerResult.FailureReason ?? "Payment refund failed.");
        }

        await PublishResultEventAsync(payment, providerResult.Succeeded, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    private Task PublishResultEventAsync(
        PaymentEntity payment,
        bool refunded,
        CancellationToken cancellationToken)
    {
        if (refunded)
        {
            return integrationEventPublisher.PublishAsync(
                new PaymentRefunded(
                    Guid.NewGuid(),
                    payment.Id,
                    payment.OrderId,
                    payment.Amount.Amount,
                    payment.Amount.Currency,
                    DateTime.UtcNow),
                cancellationToken);
        }

        return integrationEventPublisher.PublishAsync(
            new PaymentRefundFailed(
                Guid.NewGuid(),
                payment.Id,
                payment.OrderId,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.FailureReason ?? "Payment refund failed.",
                DateTime.UtcNow),
            cancellationToken);
    }
}
