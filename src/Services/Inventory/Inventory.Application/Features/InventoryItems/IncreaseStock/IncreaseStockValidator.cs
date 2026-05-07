using FluentValidation;

namespace Inventory.Application.Features.InventoryItems.IncreaseStock;

public class IncreaseStockValidator : AbstractValidator<IncreaseStockCommand>
{
    public IncreaseStockValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.ReferenceId)
            .MaximumLength(100);
    }
}
