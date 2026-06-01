using FluentValidation;

namespace Inventory.Application.Features.Reservations.ReserveOrderStock;

public sealed class ReserveOrderStockValidator : AbstractValidator<ReserveOrderStockCommand>
{
    public ReserveOrderStockValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(HaveUniqueItems)
            .WithMessage("Reservation items must be unique by product id and SKU.");

        RuleForEach(x => x.Items)
            .SetValidator(new ReserveOrderStockItemValidator());

        RuleFor(x => x.ExpiresAtUtc)
            .Must(expiresAtUtc => !expiresAtUtc.HasValue || expiresAtUtc.Value > DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future.")
            .When(x => x.ExpiresAtUtc.HasValue);
    }

    private static bool HaveUniqueItems(IReadOnlyCollection<ReserveOrderStockItem>? items)
    {
        return items is not null && items
            .Select(item => $"{item.ProductId:N}:{item.Sku?.Trim().ToUpperInvariant() ?? string.Empty}")
            .Distinct(StringComparer.Ordinal)
            .Count() == items.Count;
    }
}

public sealed class ReserveOrderStockItemValidator : AbstractValidator<ReserveOrderStockItem>
{
    public ReserveOrderStockItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Sku)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}
