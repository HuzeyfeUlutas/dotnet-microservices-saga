# Observability Standard

This document defines the repository-wide observability standard for all microservices in this solution.

The goal is to ensure that every service emits telemetry in a consistent format so that logs, metrics, and traces can be correlated across synchronous HTTP calls and asynchronous message flows.

## Observability Stack

This repository standardizes on the following stack:

- Instrumentation standard: `OpenTelemetry`
- Trace transport and collection: `OTLP` via `OpenTelemetry Collector`
- Application logging: `Serilog`
- Log storage: `Elasticsearch`
- Metrics backend: `Prometheus`
- Trace backend: `Tempo`
- Visualization and alerting: `Grafana`

Optional local development tooling:

- `Seq` may be used as a local log explorer during development

## Core Principles

- Every microservice must produce logs, metrics, and traces.
- Every request and message flow must be traceable across service boundaries.
- Observability concerns are cross-cutting concerns, not business concerns.
- Observability implementation must stay out of the Domain layer.
- The Application layer must remain infrastructure-agnostic.
- HTTP and messaging flows must use consistent correlation rules.

## Signal Roles

### Logs

Logs answer: what happened?

Use logs for:

- business-relevant events
- application errors
- unexpected exceptions
- infrastructure failures
- message publish and consume lifecycle events

Logs must be structured. Do not rely on plain string logs as the primary format.

### Metrics

Metrics answer: how much, how often, how long?

Use metrics for:

- request rate
- error rate
- latency percentiles
- queue or outbox backlog
- database and transport health indicators

Metrics are the primary source for dashboards and alerting.

### Traces

Traces answer: where did a request or message go, and where did it slow down or fail?

Use traces for:

- end-to-end HTTP request flows
- database spans
- outbound HTTP spans
- publish spans
- consume spans

Traces are the primary tool for distributed root-cause analysis.

## Health Checks

Health checks are part of the repository's operational observability standard, but they are not the same as logs, metrics, or traces.

Health checks answer:

- is the service process alive?
- is the service ready to receive traffic?
- are critical dependencies currently reachable?

Health checks should be treated as operational status signals.

### Standard Health Endpoints

HTTP API services should expose health endpoints with clear intent.

Recommended split:

- `/health/live`
- `/health/ready`

Optional:

- `/health`

### Liveness

Liveness checks answer whether the service process is alive.

Rules:

- liveness checks should be lightweight
- liveness checks should not depend on every external dependency
- liveness checks should be suitable for process supervision and restarts

Typical examples:

- application process is running
- host has started successfully

### Readiness

Readiness checks answer whether the service is ready to receive real traffic.

Rules:

- readiness checks should include critical dependencies
- readiness checks should fail when the service cannot safely handle requests
- readiness should be dependency-aware, not just process-aware

Examples for services in this repository:

- database connectivity
- message broker connectivity when messaging is a required runtime dependency

### Catalog Guidance

For `Catalog`, the readiness endpoint validates:

- PostgreSQL connectivity
- RabbitMQ availability
- Elasticsearch availability

The liveness endpoint should only indicate whether the API process is alive.

Catalog health endpoints:

```text
GET /health/live
GET /health/ready
```

Both endpoints return JSON health details including overall status, dependency entry status, duration, tags, and error message when available.

Catalog readiness is expected to be unhealthy when only database containers are running and RabbitMQ or Elasticsearch are down. That is intentional because Catalog depends on messaging and centralized logging infrastructure at runtime.

### Local Development Stack

Local Docker Compose environments may include:

- `OpenTelemetry Collector`
- `Tempo`
- `Prometheus`
- `Grafana`

This stack is intended for development and learning. Production deployments may use the same tools or managed equivalents, but the logical flow should stay the same.

## Correlation Standard

### Header Name

The standard HTTP correlation header is:

`X-Correlation-Id`

### Rules

- Every incoming HTTP request must accept `X-Correlation-Id`.
- If the header is missing, the service must generate a new correlation id.
- The service must write the correlation id back to the HTTP response header.
- The correlation id must be available throughout the request lifetime.
- The correlation id must be propagated to outgoing messages.
- Consumers must continue the correlation chain when processing messages.

### CorrelationId vs TraceId

These are not the same thing and must not be treated as interchangeable.

- `CorrelationId` tracks a business or request flow across services.
- `TraceId` tracks technical distributed tracing spans.

Both should be captured and stored when available.

## Logging Standard

### Structured Logging

All services must use structured logging with `Serilog`.

Use property-based logs, for example:

`logger.LogInformation("Product created for {ProductId}", productId);`

Avoid string-concatenated logs as the default style.

### Minimum Required Log Fields

At minimum, logs should include these fields whenever applicable:

- `ServiceName`
- `Environment`
- `CorrelationId`
- `TraceId`
- `RequestPath`
- `RequestMethod`
- `MessageId`
- `EventType`
- `EntityId` or the relevant business identifier such as `ProductId`, `OrderId`, or `InventoryItemId`

### Exception Log Enrichment

Unhandled exception logs must be enriched with request and tracing context.

At minimum, exception logs should include:

- `CorrelationId`
- `TraceId`
- `RequestPath`
- `RequestMethod`
- `ServiceName`
- `Environment`

If relevant business identifiers are already known, include them as structured properties.

### Centralized Logging

Each service produces its own logs, but logs are analyzed centrally.

Repository standard:

- Production log storage: `Elasticsearch`
- Local development option: `Seq`

Logs must not be treated as service-local files for operational troubleshooting in production.

## Metrics Standard

Metrics must be emitted through `OpenTelemetry` and exposed to `Prometheus`.

### Metrics Endpoint

HTTP API services should expose a Prometheus scrape endpoint.

Repository default:

- `/metrics`

### Baseline Metrics

Every service should expose at least:

- request count
- request duration
- request error count
- database call duration
- outbound HTTP duration when external HTTP calls exist
- message publish count
- message consume count when consumers exist

### Service-Specific Metrics

Where relevant, services may add business or operational metrics such as:

- outbox pending message count
- outbox delivery failure count
- RabbitMQ consumer lag indicators
- product creation count
- product update count

### Naming Guidance

- Prefer consistent naming by capability rather than ad hoc per service.
- Use dimensions or tags for environment, service, endpoint, message type, and result where appropriate.
- Keep high-cardinality labels under control.

Do not use user ids, raw request payload values, or other unbounded values as metric labels.

## Tracing Standard

Distributed tracing must use `OpenTelemetry`.

### Export Path

Preferred trace export path:

- application -> `OTLP` -> `OpenTelemetry Collector` -> `Tempo`

Do not couple application services directly to visualization tooling.

### Baseline Instrumentation

Every HTTP API service should include tracing for:

- ASP.NET Core incoming requests
- database access where a stable instrumentation option exists for the current repository package baseline
- outbound HTTP calls
- MassTransit publish operations
- MassTransit consume operations when consumers exist

### Trace Propagation

- Trace context must flow across HTTP boundaries.
- Trace context should also be preserved across messaging boundaries where supported by the transport and instrumentation stack.
- Traces must be exported to `Tempo`.

### Span Guidance

- Use automatic instrumentation where possible.
- Add manual spans only when automatic instrumentation does not capture an important business or technical boundary.
- Span names should be descriptive and stable.

## Messaging Observability Standard

This repository uses messaging across services, so observability must cover asynchronous flows as a first-class concern.

### Publish Correlation Propagation

When publishing a message:

- propagate the active `CorrelationId`
- set transport or framework correlation metadata when supported
- preserve tracing context
- rely on transport and instrumentation support where possible
- enrich publish logs with message metadata

### Consume Correlation Propagation

When consuming a message:

- restore the correlation chain from message headers
- start or continue the trace context
- include message metadata in structured logs

### Minimum Message Metadata

When available, capture:

- `MessageId`
- `CorrelationId`
- `ConversationId`
- message or event type
- payload `EventId`
- payload `OccurredAtUtc`

## Layer Responsibilities

### API Layer

The API layer is responsible for:

- correlation middleware
- request logging
- request and response enrichment
- response headers for correlation
- exception-to-response mapping
- health endpoint exposure

### Infrastructure Layer

The Infrastructure layer is responsible for:

- `Serilog` configuration
- `OpenTelemetry` setup
- exporters and sinks
- MassTransit telemetry integration
- message header propagation
- tracing and metric instrumentation setup

Repository rule:

- telemetry registration code such as logging sinks, trace exporters, metric exporters, and broker or transport observability integration should live in the service's `Infrastructure` project
- the API project may invoke the registration during host startup, but it should not own the implementation details for sinks, exporters, or telemetry backend wiring

### Application Layer

The Application layer should remain minimally aware of observability.

Allowed:

- `ILogger<T>` where useful
- application abstractions only when truly needed

Not allowed as a default:

- direct dependency on `Serilog`
- direct dependency on `OpenTelemetry`
- direct transport-specific correlation logic

### Domain Layer

The Domain layer must not contain observability concerns.

## Standard Response Behavior

For HTTP APIs:

- responses should include `X-Correlation-Id`
- error responses should include `traceId`
- error responses should include `correlationId` when available

This makes support and operational troubleshooting easier across environments.

## Current Repository Direction

The immediate implementation direction for services such as `Catalog`, `Inventory`, and `Order` is:

1. Add correlation middleware
2. Standardize structured logging with `Serilog`
3. Enrich exception handling with correlation and trace context
4. Propagate correlation through published messages
5. Add OpenTelemetry tracing
6. Add baseline metrics for Prometheus
7. Add liveness and readiness health checks

## Non-Goals

This document does not prescribe:

- a single vendor-managed observability platform
- exact dashboard layouts
- exact alert thresholds
- service-specific custom metric catalogs

Those can evolve later, but the telemetry contract and propagation rules in this document should remain consistent across services.
