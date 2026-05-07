using FluentValidation;

namespace Inventory.Application.Features.InventoryItems.CreateInventoryItem;

public class CreateInventoryItemValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.InitialQuantity)
            .GreaterThanOrEqualTo(0);
    }
}
