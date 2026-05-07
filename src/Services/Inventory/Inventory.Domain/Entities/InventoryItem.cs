using Inventory.Domain.Common;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities;

public class InventoryItem : AuditableEntity<Guid>
{
    private readonly List<InventoryReservation> _reservations = [];
    private readonly List<StockMovement> _stockMovements = [];

    private InventoryItem()
    {
    }

    public InventoryItem(Guid productId, string sku, int initialQuantity) : base(Guid.NewGuid())
    {
        ChangeProduct(productId);
        SetSku(sku);

        if (initialQuantity < 0)
        {
            throw new DomainException("Initial quantity cannot be negative.");
        }

        TotalQuantity = initialQuantity;
        IsActive = true;

        if (initialQuantity > 0)
        {
            AddStockMovement(StockMovementType.StockIn, initialQuantity, "Initial stock", null);
        }
    }

    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = null!;
    public int TotalQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => TotalQuantity - ReservedQuantity;
    public bool IsActive { get; private set; }
    public uint RowVersion { get; private set; }
    public IReadOnlyCollection<InventoryReservation> Reservations => _reservations.AsReadOnly();
    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();

    public void ChangeProduct(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id cannot be empty.");
        }

        ProductId = productId;
    }

    public void UpdateSku(string sku)
    {
        SetSku(sku);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void IncreaseStock(int quantity, string reason, string? referenceId = null)
    {
        EnsurePositiveQuantity(quantity);

        TotalQuantity += quantity;
        AddStockMovement(StockMovementType.StockIn, quantity, reason, referenceId);
    }

    public void AdjustStock(int newTotalQuantity, string reason, string? referenceId = null)
    {
        if (newTotalQuantity < ReservedQuantity)
        {
            throw new DomainException("Total quantity cannot be lower than reserved quantity.");
        }

        var difference = newTotalQuantity - TotalQuantity;
        TotalQuantity = newTotalQuantity;

        if (difference != 0)
        {
            AddStockMovement(StockMovementType.Adjustment, Math.Abs(difference), reason, referenceId);
        }
    }

    public InventoryReservation Reserve(Guid orderId, int quantity, DateTime reservedAtUtc, DateTime? expiresAtUtc = null)
    {
        if (!IsActive)
        {
            throw new DomainException("Inactive inventory item cannot be reserved.");
        }

        EnsurePositiveQuantity(quantity);

        if (AvailableQuantity < quantity)
        {
            throw new DomainException("Insufficient stock.");
        }

        if (_reservations.Any(x => x.OrderId == orderId && x.Status == InventoryReservationStatus.Pending))
        {
            throw new DomainException("A pending reservation already exists for this order.");
        }

        var reservation = new InventoryReservation(Id, orderId, quantity, reservedAtUtc, expiresAtUtc);

        _reservations.Add(reservation);
        ReservedQuantity += quantity;
        AddStockMovement(StockMovementType.Reserved, quantity, "Stock reserved", orderId.ToString());

        return reservation;
    }

    public void ReleaseReservation(Guid orderId, DateTime releasedAtUtc)
    {
        var reservation = GetPendingReservation(orderId);

        reservation.Release(releasedAtUtc);
        ReservedQuantity -= reservation.Quantity;
        AddStockMovement(StockMovementType.ReservationReleased, reservation.Quantity, "Reservation released", orderId.ToString());
    }

    public void CommitReservation(Guid orderId, DateTime committedAtUtc)
    {
        var reservation = GetPendingReservation(orderId);

        reservation.Confirm(committedAtUtc);
        ReservedQuantity -= reservation.Quantity;
        TotalQuantity -= reservation.Quantity;
        AddStockMovement(StockMovementType.ReservationCommitted, reservation.Quantity, "Reservation committed", orderId.ToString());
    }

    private InventoryReservation GetPendingReservation(Guid orderId)
    {
        var reservation = _reservations.SingleOrDefault(x =>
            x.OrderId == orderId &&
            x.Status == InventoryReservationStatus.Pending);

        if (reservation is null)
        {
            throw new DomainException("Pending reservation was not found.");
        }

        return reservation;
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU cannot be empty.");
        }

        Sku = sku.Trim();
    }

    private void AddStockMovement(StockMovementType type, int quantity, string reason, string? referenceId)
    {
        var movement = new StockMovement(Id, type, quantity, reason, referenceId);
        _stockMovements.Add(movement);
    }

    private static void EnsurePositiveQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }
    }
}
