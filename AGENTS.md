# Agent Instructions

## New Service Scaffolding

When creating a new service such as Inventory, Payment, Shipment, Basket, etc., do not copy the current Catalog service implementation.

Catalog may evolve over time and may contain business logic, entities, handlers, database code, endpoints, or other service-specific implementation details.

For new service scaffolding, use only this fixed template:

`docs/templates/service-skeleton.md`

The generated service must follow that template exactly unless the user explicitly requests otherwise.

Important rules:

- Do not infer the new service structure from the current state of Catalog.
- Do not add business logic during initial scaffolding.
- Do not create entities, repositories, DbContexts, migrations, handlers, controllers, endpoints, or use cases unless explicitly requested.
- Keep Domain and Application empty during initial scaffolding.
- Add the new projects to the solution.
- Configure project references exactly as described in the service skeleton template.

## Domain Common Scaffolding

When the user asks to create `BaseEntity`, `AuditableEntity`, or Domain/Common base classes for a service, use this agent skill:

`.agents/skills/dotnet-domain-common/SKILL.md`

If `docs/templates/domain-common.md` exists, treat it as the source of truth for the generated code.

Important rules:

- Do not copy Domain/Common files from another service.
- Generate files using the target service name and namespace.
- Do not add EF Core, DbContext, repositories, DTOs, handlers, controllers, endpoints, or business entities while creating these common base types.
- Do not add navigation properties to User, Seller, or another microservice entity.
- If soft delete is used, keep delete audit fields centralized in the auditable base type instead of repeating them in each entity.

## Application Layer Scaffolding

When the user asks to scaffold or implement an Application layer for a service, use:

`docs/templates/application-skeleton.md`

Important rules:

- Follow CQRS, MediatR, and FluentValidation conventions documented in this repository.
- Use feature-based organization.
- Keep `I{ServiceName}DbContext` abstractions in the Application layer and concrete DbContext implementation in Persistence.
- Do not introduce repository abstractions by default.

## Code First Persistence

This repository adopts Code First as the default persistence strategy across services.

Important rules:

- Each microservice owns its own database, DbContext, and migrations.
- Database schema changes must be introduced through EF Core migrations.
- Migrations must live in the owning `*.Persistence` project.
- Do not use manual database changes as the primary schema evolution workflow.
- Do not share tables across services.
- Keep Domain focused on business rules and entity modeling.
- Keep EF Core configuration in the Persistence layer, preferably with Fluent API configurations.
- Do not place ORM mapping, migrations, or persistence setup in API, Application, or Domain unless the user explicitly asks for an exception.

## Persistence Abstractions

This repository uses EF Core `DbContext` as the primary persistence abstraction.

Important rules:

- Do not add generic Repository or Unit of Work layers by default.
- Treat `DbContext` as the Unit of Work and `DbSet<TEntity>` as the default collection access pattern.
- Do not wrap `DbContext` with unnecessary CRUD abstractions.
- Keep queries and persistence behavior explicit and easy to trace.
- Introduce a repository only when there is a concrete, service-specific need and the reason is explicitly documented.

## Application Architecture

This repository uses CQRS, MediatR, and FluentValidation in the Application layer by default.

Important rules:

- Organize Application code by feature and use case.
- Use commands and queries for Application use cases.
- Use MediatR request/handler patterns for Application flows.
- Use FluentValidation validators for command and query validation.
- Keep validation, request models, handlers, and feature-specific response models close to the feature they belong to.
- Do not introduce generic service layers that duplicate MediatR handlers.
- Keep infrastructure and EF Core implementation details out of the Application layer.
- Prefer explicit LINQ projection for query/read models instead of mapper libraries by default.
- Do not add AutoMapper by default; introduce it only if mapping duplication becomes substantial and the user explicitly wants it.
- When a service adopts this pattern, continue using the same pattern consistently within that service.

## Exception Strategy

Use layered exception types consistently.

Important rules:

- Define `DomainException` in the Domain layer for business rule violations.
- Define Application-specific exceptions such as `NotFoundException`, `ConflictException`, and `ForbiddenException` in the Application layer.
- Use FluentValidation's `ValidationException` for request validation failures.
- Do not use `KeyNotFoundException`, `ArgumentException`, or `InvalidOperationException` as the default cross-layer exception contract for expected business/application flows.
- Map exceptions to standardized HTTP `ProblemDetails` responses in the API layer through a global exception handler.
- Include a `traceId` in error responses.

## Package Standards

Important rules:

- For this repository, use a pinned MediatR version that is suitable for learning and non-commercial use.
- Prefer `MediatR` version `12.5.0` unless the user explicitly asks to change it.
- Do not upgrade MediatR major or minor versions without explicit approval.
- Re-check package licensing and NuGet vulnerability status before changing the pinned MediatR version.
- Use a stable FluentValidation package version compatible with the service's target framework and pinned package set.

## Engineering Expectations

When making .NET backend architecture and implementation decisions:

- Act as a senior .NET backend engineer.
- Prefer pragmatic, production-oriented solutions over textbook patterns.
- Avoid unnecessary abstractions and ceremony.
- Do not introduce Repository, Unit of Work, or extra layers unless there is a clear need.
- Prefer explicit, maintainable, and debuggable code.
- Follow the architectural decisions already documented in this repository unless the user explicitly asks to change them.
- When there are multiple valid options, choose the simpler default and briefly explain the tradeoff if needed.
