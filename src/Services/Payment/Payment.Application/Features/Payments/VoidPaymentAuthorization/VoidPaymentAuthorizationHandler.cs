using Marketplace.Contracts.Payment.V1;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;
using Payment.Domain.Enums;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.VoidPaymentAuthorization;

public sealed class VoidPaymentAuthorizationHandler(
    IPaymentDbContext context,
    IPaymentProvider paymentProvider,
    IIntegrationEventPublisher integrationEventPublisher) : IRequestHandler<VoidPaymentAuthorizationCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(VoidPaymentAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException($"Payment '{request.PaymentId}' was not found.");
        }

        if (payment.Status == PaymentStatus.AuthorizationVoided)
        {
            await PublishResultEventAsync(request.RequestEventId, payment, true, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return payment.ToDto();
        }

        payment.StartAuthorizationVoid();
        var providerResult = await paymentProvider.VoidAuthorizationAsync(payment, cancellationToken);

        if (providerResult.Succeeded)
        {
            payment.MarkAuthorizationAsVoided(providerResult.ProviderTransactionId);
        }
        else
        {
            payment.MarkAuthorizationVoidAsFailed(providerResult.FailureReason ?? "Payment authorization void failed.");
        }

        await PublishResultEventAsync(request.RequestEventId, payment, providerResult.Succeeded, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentPayment = await GetCurrentPaymentAsync(request.PaymentId, cancellationToken);

            if (currentPayment?.Status == PaymentStatus.AuthorizationVoided)
            {
                return currentPayment;
            }

            throw new ConflictException($"Payment '{request.PaymentId}' authorization could not be voided due to a concurrent change.");
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Payment '{request.PaymentId}' authorization could not be voided. {exception.Message}");
        }

        return payment.ToDto();
    }

    private Task PublishResultEventAsync(
        Guid requestEventId,
        PaymentEntity payment,
        bool voided,
        CancellationToken cancellationToken)
    {
        if (voided)
        {
            return integrationEventPublisher.PublishAsync(
                new PaymentAuthorizationVoided(
                    Guid.NewGuid(),
                    requestEventId,
                    payment.Id,
                    payment.OrderId,
                    DateTime.UtcNow),
                cancellationToken);
        }

        return integrationEventPublisher.PublishAsync(
            new PaymentAuthorizationVoidFailed(
                Guid.NewGuid(),
                requestEventId,
                payment.Id,
                payment.OrderId,
                payment.FailureReason ?? "Payment authorization void failed.",
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
