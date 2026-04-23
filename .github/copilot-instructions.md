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
