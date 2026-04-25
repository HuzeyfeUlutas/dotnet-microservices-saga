using Catalog.Domain.Common;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Category : AuditableEntity<Guid>
{
    private Category()
    {
    }

    public Category(string name, string? description = null, Guid? parentCategoryId = null) : base(Guid.NewGuid())
    {
        SetName(name);
        SetDescription(description);
        ParentCategoryId = parentCategoryId;
        IsActive = true;
    }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateDetails(string name, string? description)
    {
        SetName(name);
        SetDescription(description);
    }

    public void ChangeParent(Guid? parentCategoryId)
    {
        if (parentCategoryId == Id)
        {
            throw new DomainException("Category cannot be its own parent.");
        }

        ParentCategoryId = parentCategoryId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
