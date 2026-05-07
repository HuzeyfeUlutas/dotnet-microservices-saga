using MediatR;

namespace Inventory.Application.Features.Reservations.ReleaseReservation;

public sealed record ReleaseReservationCommand(
    Guid ProductId,
    Guid OrderId) : IRequest;
