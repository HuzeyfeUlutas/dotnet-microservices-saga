# Domain Exceptions Template

This document is the source of truth for creating Domain-layer exception types in a service.

Use this template when a service needs Domain exceptions for business rule violations.

## Target Location

Create files under:

```text
src/Services/{ServiceName}/{ServiceName}.Domain/Exceptions/
```

Required initial file:

```text
DomainException.cs
```

## Rules

- Use namespace `{ServiceName}.Domain.Exceptions`.
- Create only the base `DomainException` during initial Domain exception setup.
- Throw `DomainException` for expected Domain business rule violations.
- Do not create typed Domain exceptions by default.
- Add typed Domain exceptions only when there is a concrete need for different handling, API mapping, telemetry, retry behavior, or user-facing error categorization.
- Do not use `KeyNotFoundException`, `ArgumentException`, or `InvalidOperationException` as the default contract for expected Domain business rule violations.
- Do not add Application, API, Infrastructure, or Persistence exception concerns to the Domain layer.
- Do not add HTTP status codes, `ProblemDetails`, localization, logging, or transport-specific metadata to Domain exceptions.

## DomainException.cs

```csharp
namespace {ServiceName}.Domain.Exceptions;

public class DomainException(string message) : Exception(message);
```

## Typed Exception Guidance

Typed Domain exceptions are allowed only when the additional type carries a real behavior or integration benefit.

Acceptable reasons:

- The Application layer must handle one business rule differently from another.
- The API layer must map a specific business violation to a specific response category.
- Observability needs separate metrics or structured classification for a high-value business error.
- A retry, compensation, or idempotency flow depends on the exact business violation.

Avoid typed exceptions when a clear message on `DomainException` is enough.

Example of what not to create during initial modeling:

```text
InsufficientStockException
ReservationNotFoundException
InvalidReservationStateException
ReservationAlreadyExistsException
```

These may be introduced later only if the service has a concrete handling requirement for them.
