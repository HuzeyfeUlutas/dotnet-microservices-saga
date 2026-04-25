namespace Order.Domain.Common;

public abstract class AuditableEntity<T> : BaseEntity<T>
    where T : notnull
{
    public DateTime CreatedAtUtc { get; protected set; }
    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAtUtc { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    protected AuditableEntity()
    {
    }

    protected AuditableEntity(T id) : base(id)
    {
    }
}
