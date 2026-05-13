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

        var existingReservation = _reservations.SingleOrDefault(x => x.OrderId == orderId);

        if (existingReservation is not null)
        {
            return HandleExistingReservationForReserve(existingReservation, quantity, expiresAtUtc);
        }

        if (AvailableQuantity < quantity)
        {
            throw new DomainException("Insufficient stock.");
        }

        var reservation = new InventoryReservation(Id, orderId, quantity, reservedAtUtc, expiresAtUtc);

        _reservations.Add(reservation);
        ReservedQuantity += quantity;
        AddStockMovement(StockMovementType.Reserved, quantity, "Stock reserved", orderId.ToString());

        return reservation;
    }

    public bool ReleaseReservation(Guid orderId, DateTime releasedAtUtc)
    {
        var reservation = GetReservation(orderId);

        if (reservation.Status == InventoryReservationStatus.Released ||
            reservation.Status == InventoryReservationStatus.Expired)
        {
            return false;
        }

        if (reservation.Status == InventoryReservationStatus.Confirmed)
        {
            throw new DomainException("Confirmed reservation cannot be released.");
        }

        reservation.Release(releasedAtUtc);
        ReservedQuantity -= reservation.Quantity;
        AddStockMovement(StockMovementType.ReservationReleased, reservation.Quantity, "Reservation released", orderId.ToString());

        return true;
    }

    public bool CommitReservation(Guid orderId, DateTime committedAtUtc)
    {
        var reservation = GetReservation(orderId);

        if (reservation.Status == InventoryReservationStatus.Confirmed)
        {
            return false;
        }

        if (reservation.Status == InventoryReservationStatus.Released ||
            reservation.Status == InventoryReservationStatus.Expired)
        {
            throw new DomainException("Released reservation cannot be committed.");
        }

        reservation.Confirm(committedAtUtc);
        ReservedQuantity -= reservation.Quantity;
        TotalQuantity -= reservation.Quantity;
        AddStockMovement(StockMovementType.ReservationCommitted, reservation.Quantity, "Reservation committed", orderId.ToString());

        return true;
    }

    private InventoryReservation GetReservation(Guid orderId)
    {
        var reservation = _reservations.SingleOrDefault(x => x.OrderId == orderId);

        if (reservation is null)
        {
            throw new DomainException("Reservation was not found.");
        }

        return reservation;
    }

    private static InventoryReservation HandleExistingReservationForReserve(
        InventoryReservation reservation,
        int quantity,
        DateTime? expiresAtUtc)
    {
        if (reservation.Status != InventoryReservationStatus.Pending)
        {
            throw new DomainException("Reservation already exists for this order.");
        }

        if (reservation.Quantity != quantity)
        {
            throw new DomainException("Pending reservation quantity does not match the retry request.");
        }

        if (reservation.ExpiresAtUtc != expiresAtUtc)
        {
            throw new DomainException("Pending reservation expiration does not match the retry request.");
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
