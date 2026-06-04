# Marketplace Order Platform

Marketplace Order Platform is a .NET 8 microservice sample focused on order, payment, inventory, catalog, and notification flows.

## Main Components

- `Catalog.API`: product, brand, category, and variant endpoints.
- `Inventory.API`: stock and reservation endpoints.
- `Order.API`: checkout and order orchestration endpoints.
- `Payment.API`: payment lifecycle and fake 3DS endpoints.
- `Notification.API`: notification and preference endpoints.
- `Marketplace.ApiGateway`: YARP-based API gateway for external HTTP access.

## Local Development

Use the local development runbook for day-to-day commands, ports, migrations, CI behavior, and smoke checks:

- [Local development runbook](docs/runbooks/local-development.md)

## Useful Commands

```bash
./scripts/check-local.sh
DRY_RUN=true ./scripts/apply-migrations.sh
docker compose up --build
```

## CI

GitHub Actions runs the same core local checks on push and pull request:

- restore
- build
- test
- vulnerable package audit
- migration command dry-run

See `.github/workflows/ci.yml` for the exact workflow.
