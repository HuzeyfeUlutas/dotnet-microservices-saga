using FluentValidation;

namespace Inventory.Application.Features.Reservations.CommitReservation;

public class CommitReservationValidator : AbstractValidator<CommitReservationCommand>
{
    public CommitReservationValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.OrderId)
            .NotEmpty();
    }
}
