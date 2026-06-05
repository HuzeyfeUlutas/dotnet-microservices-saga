# Frontend Demo Authentication Plan

This document defines the authentication and authorization plan for the frontend demo boundary.

The goal is to make the existing backend usable by a frontend agent through the API Gateway without turning the gateway into an internal service-to-service router.

## Scope

This plan covers:

- local Keycloak setup for demo identity
- API Gateway JWT validation
- route-level authorization policy decisions
- user identity propagation from Gateway to services
- checkout buyer identity correction
- frontend-facing endpoint expectations

This plan does not cover:

- writing the frontend application
- production identity provider deployment
- shipment, basket, invoice, or refund features
- service mesh, mTLS, or zero-trust networking implementation

## Current State

The repository already has:

- `Marketplace.ApiGateway` as the frontend HTTP entry point
- service-owned APIs for Catalog, Inventory, Order, Payment, and Notification
- direct gRPC for internal checkout dependencies
- MassTransit for asynchronous workflow progression
- local Docker Compose infrastructure
- health and Prometheus metrics endpoints

The current gap is that client-facing HTTP authorization is not defined.

Important: `docs/architecture/service-communication.md` still says the current scope does not include an API Gateway. That statement is outdated for the current repository state and should be corrected in a later documentation cleanup task.

## Target Demo Architecture

The frontend demo should use this request path:

```text
Frontend -> Marketplace.ApiGateway -> Service HTTP APIs
```

Internal service calls must keep using the existing model:

```text
Order -> Catalog gRPC
Order -> Inventory gRPC
Order -> Payment gRPC
Saga continuation -> RabbitMQ / MassTransit
```

The Gateway owns north-south client authentication and coarse route authorization.
It must not become the default route for east-west internal service calls.

## Keycloak Model

Use Keycloak as the local demo identity provider.

Recommended local settings:

| Item | Value |
| --- | --- |
| Realm | `marketplace` |
| Frontend client | `marketplace-frontend` |
| API audience | `marketplace-api` |
| Flow | Authorization Code with PKCE |
| Local issuer | `http://localhost:8086/realms/marketplace` |

The frontend client should be public. It must not use or store a client secret.

### Roles

Use realm roles for the first demo iteration.

| Role | Purpose |
| --- | --- |
| `customer` | Buyer checkout and own order/payment views |
| `admin` | Full demo administration |
| `catalog-manager` | Catalog create/update/delete |
| `inventory-manager` | Inventory stock management |
| `support` | Notification and operational support flows |

Realm roles are simpler for the demo. Client-specific roles can be introduced later if role ownership becomes more complex.

### Demo Users

Use stable users so the frontend agent and demo scripts can rely on predictable accounts.

| Username | Roles | Purpose |
| --- | --- | --- |
| `customer1` | `customer` | Checkout and order-status demo |
| `admin` | `admin` | Full access smoke/admin demo |
| `catalogmanager` | `catalog-manager` | Product/category/brand mutation demo |
| `inventorymanager` | `inventory-manager` | Stock management demo |
| `support` | `support` | Notification retry/support demo |

Passwords belong in the local Keycloak import or runbook only for demo use. They must not be reused outside local development.

## Gateway Responsibilities

The Gateway should:

- validate JWT access tokens issued by Keycloak
- keep public endpoints anonymous
- enforce role-based policies for protected route groups
- forward only trusted identity context to services
- strip any client-supplied identity headers before proxying
- expose `/health/live`, `/health/ready`, `/metrics`, and `/` anonymously
- keep CORS limited to configured frontend origins
- keep rate limiting and baseline security headers

The Gateway should not:

- implement business authorization that requires service-owned data checks
- call internal gRPC endpoints
- mutate checkout, payment, inventory, or catalog state directly
- accept user identity from client-controlled headers

## Service Responsibilities

Services should continue owning business rules.

Examples:

- Catalog owns product/category/brand rules.
- Inventory owns stock consistency and reservation invariants.
- Order owns checkout orchestration and order state.
- Payment owns payment lifecycle and provider callback handling.
- Notification owns message preference and delivery state.

Gateway route policies are coarse-grained. Service-level checks are still needed when authorization depends on resource ownership.

Example: `GET /api/orders/{orderId}` can require `customer` at the Gateway, but Order must ensure the authenticated user owns that order unless the user is `admin`.

## Identity Propagation

For the demo, the Gateway should propagate a sanitized identity context downstream.

Recommended headers:

```text
X-User-Id
X-User-Email
X-User-Roles
```

Rules:

- Gateway must remove any incoming client-supplied versions of these headers.
- Gateway must create these headers only after token validation.
- Services must treat these headers as trusted only when traffic comes through the Gateway.
- Direct service ports should not be considered protected in the demo unless they are not exposed externally.

Production alternatives include service-level JWT validation, mTLS, a service mesh, or a dedicated internal identity propagation mechanism.

## Route Authorization Matrix

This is the intended frontend demo route matrix.

| Route | Method | Auth | Roles | Notes |
| --- | --- | --- | --- | --- |
| `/` | `GET` | Anonymous | none | Gateway status |
| `/health/live` | `GET` | Anonymous | none | Health check |
| `/health/ready` | `GET` | Anonymous | none | Health check |
| `/metrics` | `GET` | Anonymous or ops-only later | none | Keep anonymous for local Prometheus |
| `/api/products` | `GET` | Anonymous | none | Public catalog browsing |
| `/api/products/{id}` | `GET` | Anonymous | none | Public catalog browsing |
| `/api/categories` | `GET` | Anonymous | none | Public catalog browsing |
| `/api/categories/{id}` | `GET` | Anonymous | none | Public catalog browsing |
| `/api/brands` | `GET` | Anonymous | none | Public catalog browsing |
| `/api/brands/{id}` | `GET` | Anonymous | none | Public catalog browsing |
| `/api/products/**` | non-GET | Protected | `admin`, `catalog-manager` | Catalog management |
| `/api/categories/**` | non-GET | Protected | `admin`, `catalog-manager` | Catalog management |
| `/api/brands/**` | non-GET | Protected | `admin`, `catalog-manager` | Catalog management |
| `/api/inventory-items/**` | any | Protected | `admin`, `inventory-manager` | Stock management; no public inventory in demo |
| `/api/checkout/pay` | `POST` | Protected | `customer`, `admin` | Buyer checkout |
| `/api/orders/{orderId}` | `GET` | Protected | `customer`, `admin` | Order must enforce ownership |
| `/api/payments/{id}` | `GET` | Protected | `customer`, `admin` | Payment must enforce ownership or order link later |
| `/api/payments/{id}/callback` | `POST` | Provider protected | none | Do not use user JWT; provider/internal protection needed |
| `/api/payments/webhooks/fake` | `POST` | Demo/provider protected | none | Dev/demo only |
| `/fake-3ds/payments/**` | any | Demo only | none or `customer` | Used to complete fake 3DS in local demo |
| `/api/notification-preferences/**` | any | Protected | authenticated | Service should enforce recipient ownership later |
| `/api/notifications` | `GET` | Protected | `admin`, `support` | Support/admin read |
| `/api/notifications/{id}` | `GET` | Protected | `admin`, `support` | Support/admin read |
| `/api/notifications/{id}/retry` | `POST` | Protected | `admin`, `support` | Support operation |

YARP route definitions may need to be split by method and path to enforce this matrix correctly.

## Payment Callback Rule

Payment provider callbacks are not frontend user actions.

They should not rely on `customer`, `admin`, or any interactive user JWT.

For the demo, fake callback routes can remain simple, but they must be clearly marked as demo-only.

Production-grade callback protection should include:

- provider-specific signature validation
- timestamp tolerance
- replay protection
- provider event id idempotency
- request body canonicalization rules
- explicit provider configuration

The existing `ProcessedProviderCallbacks` persistence concept is a good base for idempotency, but signature validation must be added before considering callbacks production-ready.

## Checkout Buyer Identity Rule

The current checkout contract includes `buyerId` in the request body.

For authenticated frontend use, the frontend must not be the source of truth for buyer identity.

Target behavior:

```text
buyer id = authenticated user id from Gateway identity context
```

The checkout request should eventually omit `buyerId`.

Temporary compatibility is acceptable during migration, but Order must reject or ignore mismatched body `buyerId` once authenticated identity is available.

## Frontend Agent Assumptions

The frontend agent should assume:

- base URL is the API Gateway
- public catalog browsing does not require a token
- checkout requires a Keycloak access token
- protected admin screens require the corresponding role
- the frontend uses Authorization Code with PKCE
- the frontend sends `Authorization: Bearer <access_token>` for protected calls
- the frontend does not send `X-User-Id`, `X-User-Email`, or `X-User-Roles`
- the frontend does not choose `buyerId` after the buyer identity migration is complete
- order confirmation is asynchronous and should be polled through `GET /api/orders/{orderId}`
- fake 3DS is local-demo behavior, not a real payment integration

A separate endpoint document should be generated after the backend route and contract changes are implemented.

Recommended output path for that later document:

```text
docs/frontend/frontend-endpoints.md
```

## Demo vs Production

### Demo Acceptable

- Keycloak runs in Docker Compose.
- Demo users and passwords are imported locally.
- Public catalog browsing is anonymous.
- Fake 3DS routes exist for local checkout completion.
- Gateway handles coarse route authorization.
- Services trust Gateway-propagated identity headers in local compose.

### Production Not Acceptable Without More Work

- Publicly exposing service ports directly.
- Trusting client-supplied identity headers.
- Using fake 3DS endpoints.
- Accepting unsigned payment callbacks.
- Using default local credentials.
- Relying only on Gateway auth when services are reachable outside the private network.

## Implementation Order

Use this order for the remaining tasks:

1. Add Keycloak to local Compose with realm import.
2. Add Gateway JWT authentication foundation.
3. Split Gateway route definitions and apply the policy matrix.
4. Add trusted identity header propagation.
5. Move checkout buyer identity from request body to authenticated identity.
6. Update smoke scripts and local runbook for Keycloak token usage.
7. Add Gateway auth tests.
8. Generate frontend endpoint documentation for the frontend agent.

Do not start frontend implementation in this repository as part of this plan.
