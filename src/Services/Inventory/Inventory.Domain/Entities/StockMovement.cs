using Inventory.Domain.Common;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities;

public class StockMovement : BaseEntity<Guid>
{
    private StockMovement()
    {
    }

    internal StockMovement(Guid inventoryItemId, StockMovementType type, int quantity, string reason, string? referenceId) : base(Guid.NewGuid())
    {
        if (inventoryItemId == Guid.Empty)
        {
            throw new DomainException("Inventory item id cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Stock movement quantity must be greater than zero.");
        }

        InventoryItemId = inventoryItemId;
        Type = type;
        Quantity = quantity;
        Reason = string.IsNullOrWhiteSpace(reason) ? type.ToString() : reason.Trim();
        ReferenceId = referenceId;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public Guid InventoryItemId { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string Reason { get; private set; } = null!;
    public string? ReferenceId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
}
