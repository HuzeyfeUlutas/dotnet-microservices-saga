using MediatR;

namespace Inventory.Application.Features.Reservations.ReleaseReservation;

public sealed record ReleaseReservationCommand(
    Guid ProductId,
    string Sku,
    Guid OrderId) : IRequest;
