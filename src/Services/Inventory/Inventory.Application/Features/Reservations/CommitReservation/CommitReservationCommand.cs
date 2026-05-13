using MediatR;

namespace Inventory.Application.Features.Reservations.CommitReservation;

public sealed record CommitReservationCommand(
    Guid ProductId,
    string Sku,
    Guid OrderId) : IRequest;
