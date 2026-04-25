using Catalog.Domain.Common;
using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class ProductVariant : AuditableEntity<Guid>
{
    private ProductVariant()
    {
    }

    public ProductVariant(Guid productId, string name, string sku) : base(Guid.NewGuid())
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id cannot be empty.");
        }

        ProductId = productId;
        SetName(name);
        SetSku(sku);
        Status = VariantStatus.Active;
    }

    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public VariantStatus Status { get; private set; }

    public void UpdateDetails(string name, string sku)
    {
        SetName(name);
        SetSku(sku);
    }

    public void Activate()
    {
        Status = VariantStatus.Active;
    }

    public void Deactivate()
    {
        Status = VariantStatus.Inactive;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Variant name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Variant SKU cannot be empty.");
        }

        Sku = sku.Trim();
    }
}
