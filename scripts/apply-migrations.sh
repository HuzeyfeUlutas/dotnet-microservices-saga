#!/usr/bin/env bash

set -euo pipefail

ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-${DOTNET_ENVIRONMENT:-Development}}"
DRY_RUN="${DRY_RUN:-false}"
NO_BUILD="${NO_BUILD:-false}"

ALL_SERVICES=(Catalog Inventory Notification Payment Order)

usage() {
  cat <<'USAGE'
Usage:
  scripts/apply-migrations.sh [service...]

Examples:
  scripts/apply-migrations.sh
  scripts/apply-migrations.sh Catalog Inventory
  SERVICES=Catalog,Inventory scripts/apply-migrations.sh
  ASPNETCORE_ENVIRONMENT=Production scripts/apply-migrations.sh Payment
  DRY_RUN=true scripts/apply-migrations.sh

Environment:
  ASPNETCORE_ENVIRONMENT / DOTNET_ENVIRONMENT  Runtime environment used by startup projects. Defaults to Development.
  SERVICES                                    Optional comma-separated service filter.
  DRY_RUN=true                               Print EF commands without applying migrations.
  NO_BUILD=true                              Add --no-build to dotnet ef database update.
USAGE
}

require_command() {
  if ! command -v "$1" >/dev/null; then
    echo "Required command was not found: $1" >&2
    exit 1
  fi
}

service_config() {
  case "$1" in
    Catalog)
      echo "src/Services/Catalog/Catalog.Persistence/Catalog.Persistence.csproj|src/Services/Catalog/Catalog.API/Catalog.API.csproj|CatalogDbContext"
      ;;
    Inventory)
      echo "src/Services/Inventory/Inventory.Persistence/Inventory.Persistence.csproj|src/Services/Inventory/Inventory.API/Inventory.API.csproj|InventoryDbContext"
      ;;
    Notification)
      echo "src/Services/Notification/Notification.Persistence/Notification.Persistence.csproj|src/Services/Notification/Notification.API/Notification.API.csproj|NotificationDbContext"
      ;;
    Payment)
      echo "src/Services/Payment/Payment.Persistence/Payment.Persistence.csproj|src/Services/Payment/Payment.API/Payment.API.csproj|PaymentDbContext"
      ;;
    Order)
      echo "src/Services/Order/Order.Persistence/Order.Persistence.csproj|src/Services/Order/Order.API/Order.API.csproj|OrderDbContext"
      ;;
    *)
      echo "Unknown service: $1" >&2
      echo "Known services: ${ALL_SERVICES[*]}" >&2
      exit 1
      ;;
  esac
}

selected_services() {
  if [[ "$#" -gt 0 ]]; then
    printf '%s\n' "$@"
    return
  fi

  if [[ -n "${SERVICES:-}" ]]; then
    echo "$SERVICES" | tr ',' '\n' | sed '/^[[:space:]]*$/d'
    return
  fi

  printf '%s\n' "${ALL_SERVICES[@]}"
}

run_migration() {
  local service="$1"
  local config project startup_project context

  config="$(service_config "$service")"
  IFS='|' read -r project startup_project context <<<"$config"

  if [[ ! -f "$project" ]]; then
    echo "Persistence project was not found for $service: $project" >&2
    exit 1
  fi

  if [[ ! -f "$startup_project" ]]; then
    echo "Startup project was not found for $service: $startup_project" >&2
    exit 1
  fi

  local command=(
    dotnet ef database update
    --project "$project"
    --startup-project "$startup_project"
    --context "$context"
  )

  if [[ "$NO_BUILD" == "true" ]]; then
    command+=(--no-build)
  fi

  echo
  echo "==> Applying migrations: $service ($context)"
  echo "Environment: $ENVIRONMENT"
  printf 'Command:'
  printf ' %q' "${command[@]}"
  echo

  if [[ "$DRY_RUN" == "true" ]]; then
    return
  fi

  ASPNETCORE_ENVIRONMENT="$ENVIRONMENT" DOTNET_ENVIRONMENT="$ENVIRONMENT" "${command[@]}"
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

require_command dotnet

if ! dotnet ef --version >/dev/null 2>&1; then
  echo "dotnet ef is not available. Install it with: dotnet tool install --global dotnet-ef" >&2
  exit 1
fi

while IFS= read -r service; do
  run_migration "$service"
done < <(selected_services "$@")
