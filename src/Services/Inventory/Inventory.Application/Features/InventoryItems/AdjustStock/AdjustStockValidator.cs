using FluentValidation;

namespace Inventory.Application.Features.InventoryItems.AdjustStock;

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .NotEmpty();

        RuleFor(x => x.NewTotalQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.ReferenceId)
            .MaximumLength(100);
    }
}
