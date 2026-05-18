using Order.Domain.Common;
using Order.Domain.Exceptions;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

public class OrderLine : BaseEntity<Guid>
{
    private OrderLine()
    {
    }

    internal OrderLine(Guid orderId, OrderLineSnapshot snapshot) : base(Guid.NewGuid())
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        OrderId = orderId;
        ProductId = snapshot.ProductId;
        Sku = snapshot.Sku;
        ProductName = snapshot.ProductName;
        VariantName = snapshot.VariantName;
        UnitPrice = snapshot.UnitPrice.Amount;
        Currency = snapshot.UnitPrice.Currency;
        Quantity = snapshot.Quantity;
        LineTotal = snapshot.UnitPrice.Multiply(snapshot.Quantity).Amount;
    }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = null!;
    public string ProductName { get; private set; } = null!;
    public string VariantName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public string Currency { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
}
