using Marketplace.Contracts.Payment.V1;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;
using Payment.Domain.Enums;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.CancelPendingPayment;

public sealed class CancelPendingPaymentHandler(
    IPaymentDbContext context,
    IIntegrationEventPublisher integrationEventPublisher) : IRequestHandler<CancelPendingPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(CancelPendingPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException($"Payment '{request.PaymentId}' was not found.");
        }

        if (payment.Status == PaymentStatus.Cancelled)
        {
            await PublishCancelledEventAsync(request.RequestEventId, payment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return payment.ToDto();
        }

        payment.CancelPending(request.Reason);

        await PublishCancelledEventAsync(request.RequestEventId, payment, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentPayment = await GetCurrentPaymentAsync(request.PaymentId, cancellationToken);

            if (currentPayment?.Status == PaymentStatus.Cancelled)
            {
                return currentPayment;
            }

            throw new ConflictException($"Payment '{request.PaymentId}' could not be cancelled due to a concurrent change.");
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Payment '{request.PaymentId}' could not be cancelled. {exception.Message}");
        }

        return payment.ToDto();
    }

    private Task PublishCancelledEventAsync(
        Guid requestEventId,
        PaymentEntity payment,
        CancellationToken cancellationToken)
    {
        return integrationEventPublisher.PublishAsync(
            new PaymentCancelled(
                Guid.NewGuid(),
                requestEventId,
                payment.Id,
                payment.OrderId,
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
