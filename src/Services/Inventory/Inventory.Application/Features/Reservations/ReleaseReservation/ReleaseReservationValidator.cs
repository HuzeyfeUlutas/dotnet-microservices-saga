using FluentValidation;

namespace Inventory.Application.Features.Reservations.ReleaseReservation;

public class ReleaseReservationValidator : AbstractValidator<ReleaseReservationCommand>
{
    public ReleaseReservationValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.OrderId)
            .NotEmpty();
    }
}
