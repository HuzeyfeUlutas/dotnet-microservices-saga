using FluentValidation;

namespace Payment.Application.Features.Payments.CancelPendingPayment;

public sealed class CancelPendingPaymentValidator : AbstractValidator<CancelPendingPaymentCommand>
{
    public CancelPendingPaymentValidator()
    {
        RuleFor(x => x.RequestEventId)
            .NotEmpty();

        RuleFor(x => x.PaymentId)
            .NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Reason)
            .NotEmpty();
    }
}
