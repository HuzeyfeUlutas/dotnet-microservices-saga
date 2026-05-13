using FluentValidation;

namespace Catalog.Application.Features.Products.GetProductPurchaseInfo;

public class GetProductPurchaseInfoValidator : AbstractValidator<GetProductPurchaseInfoQuery>
{
    public GetProductPurchaseInfoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(100);
    }
}
