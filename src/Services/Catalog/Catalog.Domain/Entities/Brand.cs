using Catalog.Domain.Common;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Brand : AuditableEntity<Guid>
{
    private Brand()
    {
    }

    public Brand(string name, string? description = null) : base(Guid.NewGuid())
    {
        SetName(name);
        SetDescription(description);
        IsActive = true;
    }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateDetails(string name, string? description)
    {
        SetName(name);
        SetDescription(description);
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
            throw new DomainException("Brand name cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
