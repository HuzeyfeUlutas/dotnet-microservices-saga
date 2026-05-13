using FluentValidation;

namespace Catalog.Application.Features.Products.ActivateProductVariant;

public class ActivateProductVariantValidator : AbstractValidator<ActivateProductVariantCommand>
{
    public ActivateProductVariantValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.VariantId)
            .NotEmpty();
    }
}
