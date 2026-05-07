namespace Inventory.Domain.Common;

public abstract class AuditableEntity<T> : BaseEntity<T>
    where T : notnull
{
    public DateTime CreatedAtUtc { get; protected set; }
    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAtUtc { get; protected set; }
    public string? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAtUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }

    protected AuditableEntity()
    {
    }

    protected AuditableEntity(T id) : base(id)
    {
    }

    public virtual void MarkAsDeleted(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
