using MediatR;
using Marketplace.Contracts.Payment.V1;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Messaging;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.CompleteFake3ds;

public class CompleteFake3dsHandler(
    IPaymentDbContext context,
    IPaymentProvider paymentProvider,
    IIntegrationEventPublisher integrationEventPublisher) : IRequestHandler<CompleteFake3dsCommand, PaymentDto>
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

        var normalizedProviderEventId = NormalizeOptional(request.ProviderEventId);

        if (normalizedProviderEventId is not null)
        {
            var callbackAlreadyProcessed = await context.ProcessedProviderCallbacks
                .AsNoTracking()
                .AnyAsync(
                    x => x.Provider == payment.Provider && x.ProviderEventId == normalizedProviderEventId,
                    cancellationToken);

            if (callbackAlreadyProcessed)
            {
                return payment.ToDto();
            }
        }

        if (payment.Status is not PaymentStatus.Pending and not PaymentStatus.RequiresAction)
        {
            return payment.ToDto();
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

        if (normalizedProviderEventId is not null)
        {
            context.ProcessedProviderCallbacks.Add(
                new ProcessedProviderCallback(
                    payment.Id,
                    payment.Provider,
                    normalizedProviderEventId));
        }

        await PublishResultEventAsync(payment, providerResult.Succeeded, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentPayment = await GetCurrentPaymentAsync(request.PaymentId, cancellationToken);

            if (currentPayment is not null)
            {
                return currentPayment;
            }

            throw new ConflictException($"Payment '{request.PaymentId}' could not be updated due to a concurrent change.");
        }
        catch (DbUpdateException) when (normalizedProviderEventId is not null)
        {
            var callbackAlreadyProcessed = await context.ProcessedProviderCallbacks
                .AsNoTracking()
                .AnyAsync(
                    x => x.Provider == payment.Provider && x.ProviderEventId == normalizedProviderEventId,
                    cancellationToken);

            if (callbackAlreadyProcessed)
            {
                var currentPayment = await GetCurrentPaymentAsync(request.PaymentId, cancellationToken);

                if (currentPayment is not null)
                {
                    return currentPayment;
                }
            }

            throw;
        }

        return payment.ToDto();
    }

    private Task PublishResultEventAsync(
        PaymentEntity payment,
        bool authorized,
        CancellationToken cancellationToken)
    {
        if (authorized)
        {
            return integrationEventPublisher.PublishAsync(
                new PaymentAuthorized(
                    Guid.NewGuid(),
                    payment.Id,
                    payment.OrderId,
                    payment.Amount.Amount,
                    payment.Amount.Currency,
                    DateTime.UtcNow),
                cancellationToken);
        }

        return integrationEventPublisher.PublishAsync(
            new PaymentAuthorizationFailed(
                Guid.NewGuid(),
                payment.Id,
                payment.OrderId,
                payment.Amount.Amount,
                payment.Amount.Currency,
                payment.FailureReason ?? "Payment authorization failed.",
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
