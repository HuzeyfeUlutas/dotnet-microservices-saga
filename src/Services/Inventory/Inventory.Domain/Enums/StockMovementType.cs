namespace Inventory.Domain.Enums;

public enum StockMovementType
{
    StockIn = 1,
    StockOut = 2,
    Reserved = 3,
    ReservationReleased = 4,
    ReservationCommitted = 5,
    Adjustment = 6
}
