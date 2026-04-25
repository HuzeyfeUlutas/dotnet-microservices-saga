# Application Skeleton Template

This document is the source of truth for creating an Application layer in a service after the initial empty service skeleton has already been created.

Use this template when the user explicitly asks to build or scaffold the Application layer for a service.

## Architecture Decision

For this repository, the Application layer uses:

- CQRS
- MediatR
- FluentValidation
- Feature-based folder organization

The Application layer must depend on the service's Domain project only. It must not contain EF Core implementation details, migrations, PostgreSQL-specific code, or infrastructure logic.

## Required Project Reference

`{ServiceName}.Application` must reference:

```text
{ServiceName}.Domain
```

## Required Package Pattern

Use stable, pinned package versions according to the repository package rules.

Typical Application-layer packages:

```text
MediatR
FluentValidation
FluentValidation.DependencyInjectionExtensions
Microsoft.EntityFrameworkCore
```

`Microsoft.EntityFrameworkCore` is allowed here only for the `I{ServiceName}DbContext` abstraction that exposes `DbSet<TEntity>` and `SaveChangesAsync`.

## Required Structure

Create the following structure:

```text
src/Services/{ServiceName}/{ServiceName}.Application
├── DependencyInjection.cs
├── Abstractions
│   ├── Persistence
│   │   └── I{ServiceName}DbContext.cs
│   └── Services
├── Features
│   ├── Products
│   │   ├── CreateProduct
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductHandler.cs
│   │   │   └── CreateProductValidator.cs
│   │   ├── GetProductById
│   │   │   ├── GetProductByIdQuery.cs
│   │   │   └── GetProductByIdHandler.cs
│   │   ├── GetProducts
│   │   │   ├── GetProductsQuery.cs
│   │   │   └── GetProductsHandler.cs
│   │   └── UpdateProduct
│   ├── Brands
│   └── Categories
├── DTOs
└── Common
```

Use the same pattern for the service's actual features. Do not hardcode `Products`, `Brands`, and `Categories` if the user is scaffolding a different bounded context.

## Required Files

### `DependencyInjection.cs`

Register:

- MediatR handlers from the Application assembly
- FluentValidation validators from the Application assembly
- validation pipeline behavior

### `Abstractions/Persistence/I{ServiceName}DbContext.cs`

Expose:

- `DbSet<TEntity>` properties needed by Application use cases
- `SaveChangesAsync`

Do not add repository interfaces by default.

### `Common`

Place cross-cutting Application concerns here, such as:

- MediatR pipeline behaviors
- shared Application exceptions
- shared request/response helpers

Typical shared Application exception types:

- `ApplicationExceptionBase`
- `NotFoundException`
- `ConflictException`
- `ForbiddenException`

### `DTOs`

Place shared DTOs here when they are used by more than one feature.

If a response model belongs only to one feature, it may stay inside that feature folder instead.

### `Features`

Organize by business feature first, then by use case.

For each use case:

- create a MediatR request
- create a handler
- add a FluentValidation validator when validation is needed
- prefer explicit LINQ projection in query handlers for read models
- if delete behavior exists, prefer soft delete command handlers over hard delete by default

## Rules

- Keep handlers focused on one use case.
- Keep request, handler, validator, and feature-specific response files close together.
- Do not put controller, endpoint, DbContext implementation, migration, or infrastructure code in the Application layer.
- Do not introduce generic service layers that duplicate MediatR handlers.
- Use the service's `I{ServiceName}DbContext` abstraction instead of referencing the concrete DbContext type.
- Use feature-based naming consistently inside the service.
- Do not add AutoMapper by default; use explicit projection unless the user explicitly asks for a mapper library.
- Use Application exceptions for expected use-case failures and leave HTTP mapping to the API layer.
