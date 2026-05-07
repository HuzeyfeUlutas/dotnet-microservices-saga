using FluentValidation;

namespace Inventory.Application.Features.Reservations.ReserveStock;

public class ReserveStockValidator : AbstractValidator<ReserveStockCommand>
{
    public ReserveStockValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.OrderId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.ExpiresAtUtc)
            .Must(expiresAtUtc => !expiresAtUtc.HasValue || expiresAtUtc.Value > DateTime.UtcNow)
            .WithMessage("Expiration date must be in the future.")
            .When(x => x.ExpiresAtUtc.HasValue);
    }
}
