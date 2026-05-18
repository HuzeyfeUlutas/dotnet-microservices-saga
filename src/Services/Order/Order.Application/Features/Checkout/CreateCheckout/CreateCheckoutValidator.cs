using FluentValidation;

namespace Order.Application.Features.Checkout.CreateCheckout;

public class CreateCheckoutValidator : AbstractValidator<CreateCheckoutCommand>
{
    public CreateCheckoutValidator()
    {
        RuleFor(x => x.BuyerId)
            .NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Method)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Items)
            .NotEmpty();

        RuleForEach(x => x.Items)
            .SetValidator(new CreateCheckoutItemValidator());
    }
}

public class CreateCheckoutItemValidator : AbstractValidator<CreateCheckoutItem>
{
    public CreateCheckoutItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}
