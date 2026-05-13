using FluentValidation;

namespace Catalog.Application.Features.Products.DeactivateProductVariant;

public class DeactivateProductVariantValidator : AbstractValidator<DeactivateProductVariantCommand>
{
    public DeactivateProductVariantValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.VariantId)
            .NotEmpty();
    }
}
