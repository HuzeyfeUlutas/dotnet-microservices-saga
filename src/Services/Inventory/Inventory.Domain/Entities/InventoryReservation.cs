using Inventory.Domain.Common;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities;

public class InventoryReservation : BaseEntity<Guid>
{
    private InventoryReservation()
    {
    }

    internal InventoryReservation(Guid inventoryItemId, Guid orderId, int quantity, DateTime reservedAtUtc, DateTime? expiresAtUtc) : base(Guid.NewGuid())
    {
        if (inventoryItemId == Guid.Empty)
        {
            throw new DomainException("Inventory item id cannot be empty.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Reservation quantity must be greater than zero.");
        }

        InventoryItemId = inventoryItemId;
        OrderId = orderId;
        Quantity = quantity;
        Status = InventoryReservationStatus.Pending;
        ReservedAtUtc = reservedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid InventoryItemId { get; private set; }
    public Guid OrderId { get; private set; }
    public int Quantity { get; private set; }
    public InventoryReservationStatus Status { get; private set; }
    public DateTime ReservedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }

    internal void Confirm(DateTime confirmedAtUtc)
    {
        EnsurePending("confirmed");

        Status = InventoryReservationStatus.Confirmed;
        ConfirmedAtUtc = confirmedAtUtc;
    }

    internal void Release(DateTime releasedAtUtc)
    {
        EnsurePending("released");

        Status = InventoryReservationStatus.Released;
        ReleasedAtUtc = releasedAtUtc;
    }

    private void EnsurePending(string action)
    {
        if (Status != InventoryReservationStatus.Pending)
        {
            throw new DomainException($"Reservation cannot be {action}.");
        }
    }
}
