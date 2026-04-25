using Catalog.Domain.Common;
using Catalog.Domain.Enums;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Product : AuditableEntity<Guid>
{
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductImage> _images = [];

    private Product()
    {
    }

    public Product(
        string name,
        string? description,
        decimal price,
        Guid brandId,
        Guid categoryId) : base(Guid.NewGuid())
    {
        SetName(name);
        SetDescription(description);
        ChangePrice(price);
        ChangeBrand(brandId);
        ChangeCategory(categoryId);
        Status = ProductStatus.Draft;
    }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public Guid BrandId { get; private set; }
    public Guid CategoryId { get; private set; }
    public ProductStatus Status { get; private set; }
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public void UpdateDetails(string name, string? description)
    {
        SetName(name);
        SetDescription(description);
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("Price cannot be negative.");
        }

        Price = price;
    }

    public void ChangeBrand(Guid brandId)
    {
        if (brandId == Guid.Empty)
        {
            throw new DomainException("Brand id cannot be empty.");
        }

        BrandId = brandId;
    }

    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new DomainException("Category id cannot be empty.");
        }

        CategoryId = categoryId;
    }

    public void Activate()
    {
        Status = ProductStatus.Active;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
    }

    public ProductVariant AddVariant(string name, string sku)
    {
        if (_variants.Any(x => string.Equals(x.Sku, sku, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("Variant SKU must be unique within the product.");
        }

        var variant = new ProductVariant(Id, name, sku);
        _variants.Add(variant);
        return variant;
    }

    public ProductImage AddImage(string imageUrl, string? altText, int sortOrder, bool isPrimary)
    {
        if (isPrimary)
        {
            foreach (var image in _images)
            {
                image.ClearPrimary();
            }
        }

        var imageEntity = new ProductImage(Id, imageUrl, altText, sortOrder, isPrimary);
        _images.Add(imageEntity);
        return imageEntity;
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.SingleOrDefault(x => x.Id == imageId);
        if (image is null)
        {
            return;
        }

        _images.Remove(image);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
