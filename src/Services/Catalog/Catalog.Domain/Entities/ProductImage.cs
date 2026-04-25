using Catalog.Domain.Common;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class ProductImage : AuditableEntity<Guid>
{
    private ProductImage()
    {
    }

    public ProductImage(Guid productId, string imageUrl, string? altText, int sortOrder, bool isPrimary) : base(Guid.NewGuid())
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id cannot be empty.");
        }

        ProductId = productId;
        SetImageUrl(imageUrl);
        SetAltText(altText);
        ChangeSortOrder(sortOrder);
        IsPrimary = isPrimary;
    }

    public Guid ProductId { get; private set; }
    public string ImageUrl { get; private set; } = null!;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    public void Update(string imageUrl, string? altText, int sortOrder)
    {
        SetImageUrl(imageUrl);
        SetAltText(altText);
        ChangeSortOrder(sortOrder);
    }

    public void MarkAsPrimary()
    {
        IsPrimary = true;
    }

    public void ClearPrimary()
    {
        IsPrimary = false;
    }

    public void ChangeSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.");
        }

        SortOrder = sortOrder;
    }

    private void SetImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new DomainException("Image URL cannot be empty.");
        }

        ImageUrl = imageUrl.Trim();
    }

    private void SetAltText(string? altText)
    {
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
    }
}
