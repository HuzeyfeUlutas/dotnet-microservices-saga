# Domain Common Template

This document is the source of truth for creating base domain common types in a service.

Use this template only when the user explicitly asks to add `BaseEntity`, `AuditableEntity`, or Domain/Common base classes to an existing service. Do not use it during initial empty service scaffolding unless the user asks for domain common types.

## Target Location

Create files under:

```text
src/Services/{ServiceName}/{ServiceName}.Domain/Common/
```

Required files:

```text
BaseEntity.cs
AuditableEntity.cs
```

## Rules

- Use namespace `{ServiceName}.Domain.Common`.
- Use generic identifier type name `T`.
- Keep setters `protected`.
- Use UTC audit fields.
- Store actor information as scalar identifiers, not entity relationships.
- Do not add `User`, `Seller`, or other microservice navigation properties.
- Do not add EF Core attributes.
- Do not add repository interfaces or implementations.
- Do not add DbContext, migrations, DTOs, validators, handlers, controllers, or endpoints.
- Keep the Domain project independent from API, Infrastructure, and Persistence.
- If the service uses soft delete, keep `IsDeleted`, `DeletedAtUtc`, and `DeletedBy` in the auditable base type.

## BaseEntity.cs

```csharp
namespace {ServiceName}.Domain.Common;

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
```

## AuditableEntity.cs

```csharp
namespace {ServiceName}.Domain.Common;

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
```

## Microservice Boundary Guidance

Audit actor fields such as `CreatedBy` and `UpdatedBy` must not create a direct relationship to an identity service table or another service's entity. Store the external actor identifier as a string or other scalar value. If a client needs actor details, compose that data through the API Gateway, BFF, or a separate query to the owning service.
