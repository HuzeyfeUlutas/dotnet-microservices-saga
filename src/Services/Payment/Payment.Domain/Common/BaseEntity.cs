namespace Payment.Domain.Common;

public abstract class BaseEntity<T>
    where T : notnull
{
    public T Id { get; protected set; } = default!;

    protected BaseEntity()
    {
    }

    protected BaseEntity(T id)
    {
        Id = id;
    }
}
