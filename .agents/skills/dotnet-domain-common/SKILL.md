---
name: dotnet-domain-common
description: Create base Domain/Common types for .NET Clean Architecture microservices. Use when asked to add BaseEntity, AuditableEntity, or common domain base classes to a service.
---

# .NET Domain Common

Use this skill to add base domain common types to a single Clean Architecture microservice.

## Workflow

1. Identify the target service name from the user request or repository path.
2. Locate the service under `src/Services/{ServiceName}`.
3. Create or update only files under `{ServiceName}.Domain/Common`.
4. If `docs/templates/domain-common.md` exists, treat it as the source of truth for generated code.
5. Use namespace `{ServiceName}.Domain.Common`.
6. Keep the Domain project independent from API, Application implementation details, Infrastructure, and Persistence.

## Required Files

Create these files when missing:

```text
src/Services/{ServiceName}/{ServiceName}.Domain/Common/BaseEntity.cs
src/Services/{ServiceName}/{ServiceName}.Domain/Common/AuditableEntity.cs
```

## Rules

- Use generic `T` for identifiers.
- Keep entity setters `protected`.
- Use UTC audit field names: `CreatedAtUtc` and `UpdatedAtUtc`.
- Store actor identifiers as scalar values such as `CreatedBy` and `UpdatedBy`.
- Do not add navigation properties to User, Seller, or another microservice entity.
- Do not add EF Core attributes, DbContext code, repository code, DTOs, validators, handlers, controllers, endpoints, or application services.
- Do not add business-specific entities while creating the common base types.
- Do not copy code from another service if the repository template exists; generate using the target service name and namespace.

## Verification

After changes, inspect the generated files and confirm:

- The namespace matches the target service.
- The Domain project still has no dependency on Infrastructure, Persistence, or API.
- No microservice boundary was crossed by adding user or seller relationships.
