# Local Development Runbook

This runbook explains how to run, validate, and troubleshoot the project locally.

## Prerequisites

Install these tools before running the full stack:

- .NET SDK 8.x
- Docker Desktop or a compatible Docker Engine
- `dotnet-ef` 8.0.4
- `curl`
- `jq`

Install EF Core CLI if it is missing:

```bash
dotnet tool install --global dotnet-ef --version 8.0.4
```

## Repository Checks

Run the same quality gate used by CI:

```bash
./scripts/check-local.sh
```

The script performs:

- `dotnet restore`
- `dotnet build --no-restore`
- `dotnet test --no-build`
- `dotnet list package --vulnerable --include-transitive`

By default, vulnerable package findings are reported. To fail the script when vulnerabilities are found:

```bash
FAIL_ON_VULNERABLE=true ./scripts/check-local.sh
```

## Docker Compose

Start the full local stack:

```bash
docker compose up --build
```

Run it in the background:

```bash
docker compose up --build -d
```

Stop containers without deleting volumes:

```bash
docker compose down
```

Stop containers and delete persisted database volumes:

```bash
docker compose down -v
```

Use `down -v` only when you intentionally want a clean local database state.

## Service Ports

Public local ports from `compose.yaml`:

| Component | URL |
| --- | --- |
| API Gateway | `http://localhost:8085` |
| Catalog API | `http://localhost:8080` |
| Inventory API | `http://localhost:8081` |
| Notification API | `http://localhost:8082` |
| Payment API | `http://localhost:8083` |
| Order API | `http://localhost:8084` |
| Keycloak | `http://localhost:8086` |
| RabbitMQ UI | `http://localhost:15672` |
| Elasticsearch | `http://localhost:9200` |
| Prometheus | `http://localhost:9090` |
| Grafana | `http://localhost:3000` |
| Tempo | `http://localhost:3200` |

Default RabbitMQ credentials:

- username: `guest`
- password: `guest`

Default Grafana credentials:

- username: `admin`
- password: `admin`

Default Keycloak admin credentials:

- username: `admin`
- password: `admin`

Keycloak demo realm:

- realm: `marketplace`
- frontend client: `marketplace-frontend`
- API audience: `marketplace-api`

Demo users use the local-only password `Password123!`:

| Username | Roles |
| --- | --- |
| `customer1` | `customer` |
| `admin` | `admin`, `customer`, `catalog-manager`, `inventory-manager`, `support` |
| `catalogmanager` | `catalog-manager` |
| `inventorymanager` | `inventory-manager` |
| `support` | `support` |

Use these accounts only for local demo work.

## Health and Metrics

Each API exposes these health endpoints:

```text
/health/live
/health/ready
```

Services with Prometheus metrics expose:

```text
/metrics
```

The gateway also exposes `/metrics` and is scraped by Prometheus.

Quick checks:

```bash
curl -fsS http://localhost:8085/health/ready
curl -fsS http://localhost:8085/metrics
curl -fsS http://localhost:9090/-/healthy
```

## API Gateway

The gateway is the preferred external HTTP entry point in local compose:

```text
http://localhost:8085
```

Configured route groups:

| Route prefix | Target service |
| --- | --- |
| `/api/brands` | Catalog |
| `/api/categories` | Catalog |
| `/api/products` | Catalog |
| `/api/inventory-items` | Inventory |
| `/api/checkout` | Order |
| `/api/orders` | Order |
| `/api/payments` | Payment |
| `/fake-3ds/payments` | Payment |
| `/api/notifications` | Notification |
| `/api/notification-preferences` | Notification |

Gateway baseline controls:

- CORS policy from `Gateway:Cors:AllowedOrigins`
- IP-based fixed-window rate limiting
- basic security headers
- forwarded header support
- Prometheus metrics endpoint

## Keycloak

The local Keycloak instance imports the `marketplace` realm from:

```text
docker/keycloak/realm-marketplace.json
```

Open the admin console at:

```text
http://localhost:8086/admin
```

The local issuer URL is:

```text
http://localhost:8086/realms/marketplace
```

For manual backend checks, request a demo access token with Resource Owner Password flow:

```bash
curl -fsS -X POST http://localhost:8086/realms/marketplace/protocol/openid-connect/token \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d 'grant_type=password' \
  -d 'client_id=marketplace-frontend' \
  -d 'username=customer1' \
  -d 'password=Password123!' |
  jq -er '.access_token'
```

The frontend demo should use Authorization Code with PKCE instead of password flow. The password flow is kept enabled only to simplify local smoke scripts and manual API checks.

## Migrations

This repository uses EF Core Code First migrations. Each service owns its own database and migrations in its own `*.Persistence` project.

Validate migration commands without applying them:

```bash
DRY_RUN=true ./scripts/apply-migrations.sh
```

Apply all service migrations to the configured environment:

```bash
./scripts/apply-migrations.sh
```

Apply selected service migrations:

```bash
./scripts/apply-migrations.sh Catalog Inventory
```

Or with an environment variable:

```bash
SERVICES=Catalog,Inventory ./scripts/apply-migrations.sh
```

Run against a specific ASP.NET environment:

```bash
ASPNETCORE_ENVIRONMENT=Production ./scripts/apply-migrations.sh Payment
```

Use `NO_BUILD=true` only after a successful build:

```bash
NO_BUILD=true ./scripts/apply-migrations.sh Order
```

Important: CI only validates migration commands with `DRY_RUN=true`. It does not apply database migrations automatically.

## CI Pipeline

The GitHub Actions workflow is located at:

```text
.github/workflows/ci.yml
```

It runs on:

- pushes to `main`, `master`, and `develop`
- pull requests targeting `main`, `master`, and `develop`
- manual `workflow_dispatch`

Pipeline steps:

1. Checkout repository.
2. Setup .NET SDK 8.x.
3. Run `./scripts/check-local.sh`.
4. Install `dotnet-ef` 8.0.4.
5. Run `DRY_RUN=true ./scripts/apply-migrations.sh`.

## Smoke Checkout

After the stack is running and migrations have been applied, run the checkout smoke flow:

```bash
./scripts/smoke-checkout.sh
```

The smoke script creates catalog data, inventory stock, checkout/order/payment records, completes fake 3DS, and waits for the order to become confirmed.

Current script defaults use the API Gateway as the external HTTP entry point:

- Gateway: `http://localhost:8085`
- Keycloak: `http://localhost:8086`

Override them if needed:

```bash
GATEWAY_URL=http://localhost:8085 \
KEYCLOAK_URL=http://localhost:8086 \
./scripts/smoke-checkout.sh
```

The script requests local demo tokens from Keycloak:

- `admin` is used for catalog and inventory setup mutations.
- `customer1` is used for checkout and order polling.

The checkout request does not send `buyerId`; Order resolves buyer identity from the Gateway-propagated `X-User-Id` header.

## Gateway Auth Smoke

Run the Gateway authorization smoke checks after the stack is running:

```bash
./scripts/smoke-gateway-auth.sh
```

The script verifies:

- public catalog browsing works without a token
- protected routes return `401` without a token
- protected routes return `403` for the wrong role
- catalog and inventory manager roles can pass their route policies
- checkout with a customer token reaches Order validation instead of failing at the Gateway

## Recommended Local Workflow

1. Run `./scripts/check-local.sh`.
2. Start infrastructure and services with `docker compose up --build -d`.
3. Apply migrations with `./scripts/apply-migrations.sh`.
4. Check readiness with `curl -fsS http://localhost:8085/health/ready`.
5. Run `./scripts/smoke-gateway-auth.sh`.
6. Run `./scripts/smoke-checkout.sh`.
7. Inspect Prometheus at `http://localhost:9090` and Grafana at `http://localhost:3000`.

## Troubleshooting

If a service is not healthy:

```bash
docker compose ps
docker compose logs <service-name>
```

If a database schema looks stale:

```bash
./scripts/apply-migrations.sh <ServiceName>
```

If you need a completely clean local database state:

```bash
docker compose down -v
docker compose up --build -d
./scripts/apply-migrations.sh
```

If package vulnerabilities appear again:

```bash
dotnet list MarketplaceOrderPlatform.sln package --vulnerable --include-transitive
```
