# Copilot Instructions

## Project Context
This is a .NET 8 microservices-based backend system for a marketplace platform.

The system includes:
- Catalog Service
- Inventory Service
- Order Service
- Payment Service
- Notification Service
- API Gateway

---

## Architecture Principles

- Each service owns its own database.
- No direct database access across services.
- No shared tables between services.
- Communication between services must be done via HTTP, gRPC or messaging.

---

## Communication Rules

- Use HTTP only for client-facing operations.
- Use RabbitMQ for service-to-service communication.
- gRPC or other messaging systems (e.g., Kafka) may be used for inter-service communication if justified by performance or contract needs.
- Order workflow must be asynchronous.
- When choosing a communication method, briefly document the rationale in code comments or service documentation.

---

## Service Responsibilities

### Catalog Service
- Product information (name, price, category)
- No stock logic

### Inventory Service
- Stock management
- Reservation and release
- Availability calculation

### Order Service
- Order creation
- Order lifecycle and state transitions

### Payment Service
- Payment result simulation

### Notification Service
- Sends notifications (can be log-based)

---

## Showing Product Stock to Clients

- Catalog Service only provides product information (name, price, category) and does NOT contain stock logic.
- Inventory Service is responsible for stock management and availability.
- If clients need to see product stock status, **do NOT add stock logic or data to Catalog Service**.
- **Best practice:** Expose a composite endpoint via the API Gateway that:
    - Fetches product data from Catalog Service
    - Fetches stock status from Inventory Service
    - Combines and returns the result to the client
- This keeps service boundaries clear and maintains single responsibility.
- If Inventory Service is temporarily unavailable, the API Gateway should handle errors gracefully (e.g., show “unknown” stock status).

---

## Workflow (Saga)

Order placement must follow this flow:

1. Order is created
2. OrderCreated event is published
3. Inventory tries to reserve stock
4. If success → payment is requested
5. If fail → order is cancelled
6. Payment processed
7. If success → order confirmed
8. If fail → inventory released + order cancelled

---

## Design Constraints

- Do not use distributed transactions
- Use eventual consistency
- Implement compensation logic (Saga)

---

## Coding Guidelines

- Use ASP.NET Core Web API
- Use clean and simple code
- Avoid over-engineering
- Prefer explicit naming
- Use async/await correctly
- Add structured logging
- Validate inputs at API boundary

---

## Restrictions

- Do NOT put stock logic inside Catalog
- Do NOT put payment logic inside Order
- Do NOT mix responsibilities across services
- Do NOT create unnecessary abstractions

---

## Output Expectations

- Generate production-ready code
- Keep implementations minimal and clear
- Avoid placeholder architecture
- Focus on correctness over complexity


---


## New Service Scaffolding

When generating a new service, do not use the current Catalog implementation as the source of truth.

Catalog may change over time and may contain real business code.

Use the fixed initial service template located at:

`docs/templates/service-skeleton.md`

Follow that template exactly for services such as Inventory, Payment, Shipment, Basket, etc.

Do not create extra folders, entities, repositories, DbContexts, migrations, handlers, controllers, endpoints, or business logic unless explicitly requested.

Domain and Application must remain empty for initial scaffolding.

---

## Agent Skills

For detailed, task-specific instructions, use repository skills under:

`.agents/skills`

When asked to create `BaseEntity`, `AuditableEntity`, or Domain/Common base classes for a service, use:

`.agents/skills/dotnet-domain-common/SKILL.md`

If `docs/templates/domain-common.md` exists, treat it as the source of truth for generated code.

---

## Application Layer Scaffolding

When asked to scaffold or implement an Application layer for a service, use:

`docs/templates/application-skeleton.md`

---

## Clean Architecture Layering

- Each service must be separated into the following layers according to Clean Architecture principles:
    - API
    - Application
    - Domain
    - Infrastructure
    - Persistence
- Dependencies between layers should only be top-down.
- Each service should have its own independent Clean Architecture structure.
- Here, Clean Architecture dependency rules must be preserved; it is important in which layer a NuGet package is installed.

---

## Application Layer Standards

- Use CQRS in the Application layer.
- Use MediatR request and handler patterns for Application use cases.
- Use FluentValidation for command and query validation.
- Prefer feature-based organization in the Application project.
- Keep validators, requests, handlers, and feature-specific response models close to the feature they belong to.
- Do not place EF Core implementation details or infrastructure logic in the Application layer.

---

## Package Standards

- For this repository, prefer `MediatR` version `12.5.0` unless the user explicitly asks to change it.
- Do not upgrade MediatR major or minor versions without explicit approval.
- Re-check package licensing and NuGet vulnerability status before changing the pinned MediatR version.
- Use stable, pinned package versions for FluentValidation and related Application-layer dependencies.

---

## Exception Handling

- Define domain rule violations in the Domain layer with a dedicated `DomainException`.
- Define Application-layer exceptions such as not found, conflict, and forbidden cases in the Application layer.
- Use a global exception handler in the API layer.
- Return standardized `ProblemDetails` responses and include a `traceId`.

---

## Code Comment Standards

- For every important function, class, and complex workflow, **add Turkish explanation comments**.
- Comments should explain not only what the code does, but also **why** it is done that way.
- Especially for infrastructure or technology choices (e.g., Redis cache, InMemory cache), the reason for the choice should be stated.

**Comment examples:**

```csharp
// Order data is kept in Redis cache for fast access.
// Redis is preferred because a shared and persistent cache is needed between multiple services.
private readonly IDistributedCache _cache;

// InMemory cache is used for user sessions.
// Because session data will only be kept on a single instance and performance is a priority.
private readonly IMemoryCache _memoryCache;

// This function initiates the stock reservation for the order.
// If the stock is insufficient, the order is cancelled. It is asynchronous because messaging with Inventory Service is required.
public async Task ReserveStockAsync(Order order) { ... }
```
