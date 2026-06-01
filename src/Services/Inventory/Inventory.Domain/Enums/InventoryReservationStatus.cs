namespace Inventory.Domain.Enums;

public enum InventoryReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    Released = 3,
    Expired = 4,
    CommitReversed = 5
}
