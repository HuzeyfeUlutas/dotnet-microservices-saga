using FluentValidation;

namespace Payment.Application.Features.Payments.CreatePayment;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(150);
    }
}
