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
        var existingPayment = await context.Payments
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);

        if (existingPayment is not null)
        {
            var existingAction = ResolveExistingAction(existingPayment);
            return new CreatePaymentResultDto(existingPayment.ToDto(), existingAction);
        }

        var payment = new PaymentEntity(
            request.OrderId,
            new Money(request.Amount, request.Currency),
            request.Provider,
            request.Method,
            request.IdempotencyKey);

        payment.StartAuthorization(request.IdempotencyKey);
        var action = await paymentProvider.StartAuthorizationAsync(payment, cancellationToken);
        payment.RequireAction(providerActionReference: action.RedirectUrl);

        context.Payments.Add(payment);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Payment could not be created. {exception.Message}");
        }

        return new CreatePaymentResultDto(payment.ToDto(), action);
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
