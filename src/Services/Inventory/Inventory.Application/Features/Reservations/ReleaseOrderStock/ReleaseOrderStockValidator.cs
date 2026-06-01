using FluentValidation;

namespace Inventory.Application.Features.Reservations.ReleaseOrderStock;

public sealed class ReleaseOrderStockValidator : AbstractValidator<ReleaseOrderStockCommand>
{
    public ReleaseOrderStockValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(HaveUniqueItems)
            .WithMessage("Stock release items must be unique by product id and SKU.");

        RuleForEach(x => x.Items)
            .SetValidator(new ReleaseOrderStockItemValidator());
    }

    private static bool HaveUniqueItems(IReadOnlyCollection<ReleaseOrderStockItem>? items)
    {
        return items is not null && items
            .Select(item => $"{item.ProductId:N}:{item.Sku?.Trim().ToUpperInvariant() ?? string.Empty}")
            .Distinct(StringComparer.Ordinal)
            .Count() == items.Count;
    }
}

public sealed class ReleaseOrderStockItemValidator : AbstractValidator<ReleaseOrderStockItem>
{
    public ReleaseOrderStockItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty();
    }
}
