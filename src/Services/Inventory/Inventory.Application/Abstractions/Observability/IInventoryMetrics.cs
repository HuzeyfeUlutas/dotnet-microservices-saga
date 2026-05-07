namespace Inventory.Application.Abstractions.Observability;

public interface IInventoryMetrics
{
    void RecordInventoryItemCreated();
    void RecordStockIncreased();
    void RecordStockAdjusted();
    void RecordStockReserved();
    void RecordReservationCommitted();
    void RecordReservationReleased();
    void RecordStockUnavailable(string reason);
}
