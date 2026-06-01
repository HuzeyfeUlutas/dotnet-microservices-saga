using FluentValidation;

namespace Inventory.Application.Features.Reservations.CommitOrderStock;

public sealed class CommitOrderStockValidator : AbstractValidator<CommitOrderStockCommand>
{
    public CommitOrderStockValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(HaveUniqueItems)
            .WithMessage("Stock commit items must be unique by product id and SKU.");

        RuleForEach(x => x.Items)
            .SetValidator(new CommitOrderStockItemValidator());
    }

    private static bool HaveUniqueItems(IReadOnlyCollection<CommitOrderStockItem>? items)
    {
        return items is not null && items
            .Select(item => $"{item.ProductId:N}:{item.Sku?.Trim().ToUpperInvariant() ?? string.Empty}")
            .Distinct(StringComparer.Ordinal)
            .Count() == items.Count;
    }
}

public sealed class CommitOrderStockItemValidator : AbstractValidator<CommitOrderStockItem>
{
    public CommitOrderStockItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty();
    }
}
