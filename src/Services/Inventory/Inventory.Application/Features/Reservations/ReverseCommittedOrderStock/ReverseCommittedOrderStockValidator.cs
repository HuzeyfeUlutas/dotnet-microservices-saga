using FluentValidation;

namespace Inventory.Application.Features.Reservations.ReverseCommittedOrderStock;

public sealed class ReverseCommittedOrderStockValidator : AbstractValidator<ReverseCommittedOrderStockCommand>
{
    public ReverseCommittedOrderStockValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(HaveUniqueItems)
            .WithMessage("Committed stock reverse items must be unique by product id and SKU.");

        RuleForEach(x => x.Items)
            .SetValidator(new ReverseCommittedOrderStockItemValidator());
    }

    private static bool HaveUniqueItems(IReadOnlyCollection<ReverseCommittedOrderStockItem>? items)
    {
        return items is not null && items
            .Select(item => $"{item.ProductId:N}:{item.Sku?.Trim().ToUpperInvariant() ?? string.Empty}")
            .Distinct(StringComparer.Ordinal)
            .Count() == items.Count;
    }
}

public sealed class ReverseCommittedOrderStockItemValidator : AbstractValidator<ReverseCommittedOrderStockItem>
{
    public ReverseCommittedOrderStockItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty();
    }
}
