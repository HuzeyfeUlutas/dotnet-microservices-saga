using MediatR;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions.Persistence;
using Payment.Application.Abstractions.Providers;
using Payment.Application.Common.Exceptions;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Features.Payments.CreatePayment;

public class CreatePaymentHandler(
    IPaymentDbContext context,
    IPaymentProvider paymentProvider) : IRequestHandler<CreatePaymentCommand, CreatePaymentResultDto>
{
    public async Task<CreatePaymentResultDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        var amount = new Money(request.Amount, request.Currency);

        var existingPayment = await context.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existingPayment is not null)
        {
            return ResolveIdempotentResult(existingPayment, request, amount);
        }

        var payment = new PaymentEntity(
            request.OrderId,
            amount,
            request.Provider,
            request.Method,
            idempotencyKey);

        payment.StartAuthorization(idempotencyKey);
        var action = await paymentProvider.StartAuthorizationAsync(payment, cancellationToken);
        payment.RequireAction(providerActionReference: action.RedirectUrl);

        context.Payments.Add(payment);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            var concurrentPayment = await context.Payments
                .AsNoTracking()
                .Include(x => x.Attempts)
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

            if (concurrentPayment is not null)
            {
                return ResolveIdempotentResult(concurrentPayment, request, amount);
            }

            throw new ConflictException($"Payment could not be created. {exception.Message}");
        }

        return new CreatePaymentResultDto(payment.ToDto(), action);
    }

    private static string NormalizeIdempotencyKey(string idempotencyKey)
    {
        return idempotencyKey.Trim();
    }

    private static CreatePaymentResultDto ResolveIdempotentResult(
        PaymentEntity payment,
        CreatePaymentCommand request,
        Money amount)
    {
        EnsureSameCreateRequest(payment, request, amount);

        var existingAction = ResolveExistingAction(payment);
        return new CreatePaymentResultDto(payment.ToDto(), existingAction);
    }

    private static void EnsureSameCreateRequest(PaymentEntity payment, CreatePaymentCommand request, Money amount)
    {
        if (payment.OrderId != request.OrderId ||
            payment.Amount.Amount != amount.Amount ||
            payment.Amount.Currency != amount.Currency ||
            payment.Provider != request.Provider ||
            payment.Method != request.Method)
        {
            throw new ConflictException(
                "Idempotency key is already associated with a different payment create request.");
        }
    }

    private static PaymentActionDto ResolveExistingAction(PaymentEntity payment)
    {
        if (payment.Status == PaymentStatus.RequiresAction)
        {
            var latestAuthorizationAttempt = payment.Attempts
                .Where(x => x.Type == PaymentAttemptType.Authorization)
                .OrderByDescending(x => x.AttemptNumber)
                .FirstOrDefault();

            return new PaymentActionDto(
                Type: "Redirect",
                RedirectUrl: latestAuthorizationAttempt?.ProviderActionReference);
        }

        return new PaymentActionDto(Type: "None");
    }
}
