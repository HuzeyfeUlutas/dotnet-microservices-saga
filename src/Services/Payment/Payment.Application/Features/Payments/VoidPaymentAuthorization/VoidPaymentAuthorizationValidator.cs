using FluentValidation;

namespace Payment.Application.Features.Payments.VoidPaymentAuthorization;

public sealed class VoidPaymentAuthorizationValidator : AbstractValidator<VoidPaymentAuthorizationCommand>
{
    public VoidPaymentAuthorizationValidator()
    {
        RuleFor(x => x.RequestEventId)
            .NotEmpty();

        RuleFor(x => x.PaymentId)
            .NotEmpty();
    }
}
