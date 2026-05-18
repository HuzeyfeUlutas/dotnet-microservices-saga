using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.Contracts.IntegrationEvents;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;
using Payment.Domain.Enums;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.CapturePayment;

public class CapturePaymentHandler(
    IPaymentDbContext context,
    IPaymentProvider paymentProvider,
    IIntegrationEventPublisher integrationEventPublisher) : IRequestHandler<CapturePaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException($"Payment '{request.PaymentId}' was not found.");
        }

        if (payment.Status == PaymentStatus.Captured)
        {
            return payment.ToDto();
        }

        payment.StartCapture();
        var providerResult = await paymentProvider.CaptureAsync(payment, cancellationToken);

        if (providerResult.Succeeded)
        {
            payment.MarkAsCaptured(providerResult.ProviderTransactionId);
        }
        else
        {
            payment.MarkCaptureAsFailed(providerResult.FailureReason ?? "Payment capture failed.");
        }

        await PublishResultEventAsync(payment, providerResult.Succeeded, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentPayment = await GetCurrentPaymentAsync(request.PaymentId, cancellationToken);

            if (currentPayment?.Status == PaymentStatus.Captured)
            {
                return currentPayment;
            }

            throw new ConflictException($"Payment '{request.PaymentId}' could not be captured due to a concurrent change.");
        }

        return payment.ToDto();
    }

    private Task PublishResultEventAsync(
        PaymentEntity payment,
        bool captured,
        CancellationToken cancellationToken)
    {
        if (captured)
        {
            return integrationEventPublisher.PublishAsync(
                new PaymentCaptured(
                    Guid.NewGuid(),
                    payment.Id,
                    payment.OrderId,
                    payment.Amount.Amount,
                    payment.Amount.Currency,
                    DateTime.UtcNow),
                cancellationToken);
        }

        return integrationEventPublisher.PublishAsync(
            new PaymentCaptureFailed(
                Guid.NewGuid(),
                payment.Id,
                payment.OrderId,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.FailureReason ?? "Payment capture failed.",
                DateTime.UtcNow),
            cancellationToken);
    }

    private Task<PaymentDto?> GetCurrentPaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        return context.Payments
            .AsNoTracking()
            .Where(x => x.Id == paymentId)
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
    }
}
