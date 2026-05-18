using Order.Domain.Exceptions;

namespace Order.Domain.ValueObjects;

public sealed record OrderLineSnapshot
{
    public OrderLineSnapshot(
        Guid productId,
        string sku,
        string productName,
        string variantName,
        Money unitPrice,
        int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id cannot be empty.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        ProductId = productId;
        Sku = NormalizeRequired(sku, "Sku cannot be empty.");
        ProductName = NormalizeRequired(productName, "Product name cannot be empty.");
        VariantName = NormalizeRequired(variantName, "Variant name cannot be empty.");
        UnitPrice = unitPrice ?? throw new DomainException("Unit price cannot be empty.");
        Quantity = quantity;
    }

    public Guid ProductId { get; }
    public string Sku { get; }
    public string ProductName { get; }
    public string VariantName { get; }
    public Money UnitPrice { get; }
    public int Quantity { get; }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message);
        }

        return value.Trim();
    }
}
