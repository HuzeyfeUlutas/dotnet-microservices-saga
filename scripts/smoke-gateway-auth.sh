#!/usr/bin/env bash

set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:8085}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8086}"
KEYCLOAK_REALM="${KEYCLOAK_REALM:-marketplace}"
KEYCLOAK_CLIENT_ID="${KEYCLOAK_CLIENT_ID:-marketplace-frontend}"
SMOKE_CUSTOMER_USERNAME="${SMOKE_CUSTOMER_USERNAME:-customer1}"
SMOKE_CUSTOMER_PASSWORD="${SMOKE_CUSTOMER_PASSWORD:-Password123!}"
SMOKE_CATALOG_MANAGER_USERNAME="${SMOKE_CATALOG_MANAGER_USERNAME:-catalogmanager}"
SMOKE_CATALOG_MANAGER_PASSWORD="${SMOKE_CATALOG_MANAGER_PASSWORD:-Password123!}"
SMOKE_INVENTORY_MANAGER_USERNAME="${SMOKE_INVENTORY_MANAGER_USERNAME:-inventorymanager}"
SMOKE_INVENTORY_MANAGER_PASSWORD="${SMOKE_INVENTORY_MANAGER_PASSWORD:-Password123!}"

require_command() {
  if ! command -v "$1" >/dev/null; then
    echo "Required command was not found: $1" >&2
    exit 1
  fi
}

wait_ready() {
  local url="$1"

  for _ in $(seq 1 30); do
    if curl -fsS "$url/health/ready" >/dev/null; then
      return
    fi

    sleep 1
  done

  echo "Readiness check timed out: $url" >&2
  exit 1
}

uuid() {
  uuidgen | tr '[:upper:]' '[:lower:]'
}

request_token() {
  local username="$1"
  local password="$2"

  curl -fsS -X POST "$KEYCLOAK_URL/realms/$KEYCLOAK_REALM/protocol/openid-connect/token" \
    -H 'Content-Type: application/x-www-form-urlencoded' \
    -d 'grant_type=password' \
    -d "client_id=$KEYCLOAK_CLIENT_ID" \
    --data-urlencode "username=$username" \
    --data-urlencode "password=$password" |
    jq -er '.access_token'
}

get_token() {
  local username="$1"
  local password="$2"

  for _ in $(seq 1 30); do
    if token="$(request_token "$username" "$password" 2>/dev/null)"; then
      echo "$token"
      return
    fi

    sleep 1
  done

  echo "Token request timed out for user: $username" >&2
  exit 1
}

http_status() {
  local method="$1"
  local url="$2"
  local token="${3:-}"
  local payload="${4:-}"
  local body_file
  local status

  body_file="$(mktemp)"

  local args=(-sS -o "$body_file" -w "%{http_code}" -X "$method" "$url")

  if [[ -n "$token" ]]; then
    args+=(-H "Authorization: Bearer $token")
  fi

  if [[ -n "$payload" ]]; then
    args+=(-H 'Content-Type: application/json' -d "$payload")
  fi

  if ! status="$(curl "${args[@]}")"; then
    echo "HTTP request failed: $method $url" >&2
    cat "$body_file" >&2 || true
    rm -f "$body_file"
    exit 1
  fi

  rm -f "$body_file"
  echo "$status"
}

expect_status() {
  local name="$1"
  local expected="$2"
  local method="$3"
  local url="$4"
  local token="${5:-}"
  local payload="${6:-}"
  local actual

  actual="$(http_status "$method" "$url" "$token" "$payload")"

  if [[ "$actual" != "$expected" ]]; then
    echo "FAIL $name: expected $expected, got $actual" >&2
    exit 1
  fi

  echo "PASS $name -> $actual"
}

require_command curl
require_command jq
require_command mktemp
require_command seq
require_command uuidgen

wait_ready "$GATEWAY_URL"

customer_token="$(get_token "$SMOKE_CUSTOMER_USERNAME" "$SMOKE_CUSTOMER_PASSWORD")"
catalog_manager_token="$(get_token "$SMOKE_CATALOG_MANAGER_USERNAME" "$SMOKE_CATALOG_MANAGER_PASSWORD")"
inventory_manager_token="$(get_token "$SMOKE_INVENTORY_MANAGER_USERNAME" "$SMOKE_INVENTORY_MANAGER_PASSWORD")"

suffix="$(uuid)"
brand_payload="$(jq -nc --arg name "Auth Smoke Brand $suffix" '{name:$name,description:"gateway auth smoke"}')"
invalid_checkout_payload='{}'

expect_status "public catalog GET is anonymous" 200 GET "$GATEWAY_URL/api/products"
expect_status "catalog mutation without token is unauthorized" 401 POST "$GATEWAY_URL/api/brands" "" "$brand_payload"
expect_status "catalog mutation with customer role is forbidden" 403 POST "$GATEWAY_URL/api/brands" "$customer_token" "$brand_payload"
expect_status "catalog mutation with catalog-manager role reaches service" 201 POST "$GATEWAY_URL/api/brands" "$catalog_manager_token" "$brand_payload"
expect_status "inventory route without token is unauthorized" 401 GET "$GATEWAY_URL/api/inventory-items"
expect_status "inventory route with customer role is forbidden" 403 GET "$GATEWAY_URL/api/inventory-items" "$customer_token"
expect_status "inventory route with inventory-manager role reaches service" 200 GET "$GATEWAY_URL/api/inventory-items" "$inventory_manager_token"
expect_status "checkout without token is unauthorized" 401 POST "$GATEWAY_URL/api/checkout/pay" "" "$invalid_checkout_payload"
expect_status "checkout with catalog-manager role is forbidden" 403 POST "$GATEWAY_URL/api/checkout/pay" "$catalog_manager_token" "$invalid_checkout_payload"
expect_status "checkout with customer role reaches order validation" 400 POST "$GATEWAY_URL/api/checkout/pay" "$customer_token" "$invalid_checkout_payload"

echo "Gateway auth smoke completed successfully."
