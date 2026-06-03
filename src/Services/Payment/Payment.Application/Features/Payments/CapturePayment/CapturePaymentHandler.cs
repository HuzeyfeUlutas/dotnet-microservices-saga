using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;
using Payment.Domain.Enums;

namespace Payment.Application.Features.Payments.CapturePayment;

public class CapturePaymentHandler(
    IPaymentDbContext context,
    IPaymentProvider paymentProvider,
    ILogger<CapturePaymentHandler> logger) : IRequestHandler<CapturePaymentCommand, PaymentDto>
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

        var attempt = payment.StartCapture();
        context.PaymentAttempts.Add(attempt);
        var providerResult = await paymentProvider.CaptureAsync(payment, cancellationToken);

        if (providerResult.Succeeded)
        {
            payment.MarkAsCaptured(providerResult.ProviderTransactionId);
        }
        else
        {
            payment.MarkCaptureAsFailed(providerResult.FailureReason ?? "Payment capture failed.");
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var concurrencyEntries = await DescribeConcurrencyEntriesAsync(exception, cancellationToken);
            logger.LogWarning(
                exception,
                "Payment {PaymentId} capture concurrency conflict. Entries: {ConcurrencyEntries}",
                request.PaymentId,
                concurrencyEntries);

            var currentPayment = await GetCurrentPaymentAsync(request.PaymentId, cancellationToken);

            if (currentPayment?.Status == PaymentStatus.Captured)
            {
                return currentPayment;
            }

            throw new ConflictException($"Payment '{request.PaymentId}' could not be captured due to a concurrent change.");
        }

        return payment.ToDto();
    }

    private static async Task<string> DescribeConcurrencyEntriesAsync(
        DbUpdateConcurrencyException exception,
        CancellationToken cancellationToken)
    {
        var descriptions = new List<string>();

        foreach (var entry in exception.Entries)
        {
            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
            var concurrencyTokens = entry.Properties
                .Where(property => property.Metadata.IsConcurrencyToken)
                .Select(property =>
                    $"{property.Metadata.Name} " +
                    $"original={FormatValue(property.OriginalValue)} " +
                    $"current={FormatValue(property.CurrentValue)} " +
                    $"database={FormatValue(databaseValues?[property.Metadata.Name])}");

            descriptions.Add($"{entry.Metadata.ClrType.Name}[{string.Join(", ", concurrencyTokens)}]");
        }

        return string.Join("; ", descriptions);
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            byte[] bytes => Convert.ToHexString(bytes),
            _ => value.ToString() ?? "<null>"
        };
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
