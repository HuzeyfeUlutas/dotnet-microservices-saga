using System.Diagnostics.Metrics;
using Inventory.Application.Abstractions.Observability;

namespace Inventory.Infrastructure.Observability;

public sealed class InventoryMetrics : IInventoryMetrics
{
    public const string MeterName = "MarketplaceOrderPlatform.Inventory";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> InventoryItemCreatedCounter =
        Meter.CreateCounter<long>("inventory.items.created");
    private static readonly Counter<long> StockIncreasedCounter =
        Meter.CreateCounter<long>("inventory.stock.increased");
    private static readonly Counter<long> StockAdjustedCounter =
        Meter.CreateCounter<long>("inventory.stock.adjusted");
    private static readonly Counter<long> StockReservedCounter =
        Meter.CreateCounter<long>("inventory.stock.reserved");
    private static readonly Counter<long> ReservationCommittedCounter =
        Meter.CreateCounter<long>("inventory.reservations.committed");
    private static readonly Counter<long> ReservationReleasedCounter =
        Meter.CreateCounter<long>("inventory.reservations.released");
    private static readonly Counter<long> StockUnavailableCounter =
        Meter.CreateCounter<long>("inventory.stock.unavailable");

    public void RecordInventoryItemCreated()
    {
        InventoryItemCreatedCounter.Add(1);
    }

    public void RecordStockIncreased()
    {
        StockIncreasedCounter.Add(1);
    }

    public void RecordStockAdjusted()
    {
        StockAdjustedCounter.Add(1);
    }

    public void RecordStockReserved()
    {
        StockReservedCounter.Add(1);
    }

    public void RecordReservationCommitted()
    {
        ReservationCommittedCounter.Add(1);
    }

    public void RecordReservationReleased()
    {
        ReservationReleasedCounter.Add(1);
    }

    public void RecordStockUnavailable(string reason)
    {
        StockUnavailableCounter.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }
}
