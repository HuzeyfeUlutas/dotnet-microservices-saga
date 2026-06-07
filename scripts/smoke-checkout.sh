#!/usr/bin/env bash

set -euo pipefail

GATEWAY_URL="${GATEWAY_URL:-http://localhost:8085}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8086}"
KEYCLOAK_REALM="${KEYCLOAK_REALM:-marketplace}"
KEYCLOAK_CLIENT_ID="${KEYCLOAK_CLIENT_ID:-marketplace-frontend}"
SMOKE_ADMIN_USERNAME="${SMOKE_ADMIN_USERNAME:-admin}"
SMOKE_ADMIN_PASSWORD="${SMOKE_ADMIN_PASSWORD:-Password123!}"
SMOKE_CUSTOMER_USERNAME="${SMOKE_CUSTOMER_USERNAME:-customer1}"
SMOKE_CUSTOMER_PASSWORD="${SMOKE_CUSTOMER_PASSWORD:-Password123!}"
HEALTH_URLS="${HEALTH_URLS:-$GATEWAY_URL http://localhost:8080 http://localhost:8081 http://localhost:8083 http://localhost:8084}"

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

require_command curl
require_command jq
require_command seq
require_command uuidgen

for url in $HEALTH_URLS; do
  wait_ready "$url"
done

admin_token="$(get_token "$SMOKE_ADMIN_USERNAME" "$SMOKE_ADMIN_PASSWORD")"
customer_token="$(get_token "$SMOKE_CUSTOMER_USERNAME" "$SMOKE_CUSTOMER_PASSWORD")"

suffix="$(uuid)"
sku="SMOKE-$suffix"

brand_id="$(
  curl -fsS -X POST "$GATEWAY_URL/api/brands" \
    -H "Authorization: Bearer $admin_token" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg name "Smoke Brand $suffix" '{name:$name,description:"compose smoke"}')" |
    jq -er '.id'
)"
category_id="$(
  curl -fsS -X POST "$GATEWAY_URL/api/categories" \
    -H "Authorization: Bearer $admin_token" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg name "Smoke Category $suffix" '{name:$name,description:"compose smoke",parentCategoryId:null}')" |
    jq -er '.id'
)"
product_id="$(
  curl -fsS -X POST "$GATEWAY_URL/api/products" \
    -H "Authorization: Bearer $admin_token" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg name "Smoke Product $suffix" --arg brand "$brand_id" --arg category "$category_id" '{name:$name,description:"compose smoke",price:199.90,brandId:$brand,categoryId:$category}')" |
    jq -er '.id'
)"
variant_id="$(
  curl -fsS -X POST "$GATEWAY_URL/api/products/$product_id/variants" \
    -H "Authorization: Bearer $admin_token" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg sku "$sku" '{name:"Default",sku:$sku}')" |
    jq -er '.id'
)"

curl -fsS -X PUT "$GATEWAY_URL/api/products/$product_id" \
  -H "Authorization: Bearer $admin_token" \
  -H 'Content-Type: application/json' \
  -d "$(jq -nc --arg name "Smoke Product $suffix" --arg brand "$brand_id" --arg category "$category_id" '{name:$name,description:"compose smoke",price:199.90,brandId:$brand,categoryId:$category,status:2}')" \
  >/dev/null
curl -fsS -X POST "$GATEWAY_URL/api/products/$product_id/variants/$variant_id/activate" \
  -H "Authorization: Bearer $admin_token" \
  >/dev/null

inventory_id="$(
  curl -fsS -X POST "$GATEWAY_URL/api/inventory-items" \
    -H "Authorization: Bearer $admin_token" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg product "$product_id" --arg sku "$sku" '{productId:$product,sku:$sku,initialQuantity:5}')" |
    jq -er '.id'
)"
checkout="$(
  curl -fsS -X POST "$GATEWAY_URL/api/checkout/pay" \
    -H "Authorization: Bearer $customer_token" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg product "$product_id" --arg sku "$sku" --arg idem "checkout-$suffix" '{items:[{productId:$product,sku:$sku,quantity:2}],idempotencyKey:$idem,provider:"Fake",method:"Card"}')"
)"
order_id="$(jq -er '.orderId' <<<"$checkout")"
payment_id="$(jq -er '.payment.paymentId' <<<"$checkout")"

echo "gateway=$GATEWAY_URL"
echo "keycloak=$KEYCLOAK_URL/realms/$KEYCLOAK_REALM"
echo "product=$product_id"
echo "variant=$variant_id"
echo "inventory=$inventory_id"
echo "order=$order_id"
echo "payment=$payment_id"
echo "action=$(jq -er '.payment.action.type' <<<"$checkout")"
echo "redirect=$(jq -er '.payment.action.redirectUrl' <<<"$checkout")"

curl -fsS -X POST "$GATEWAY_URL/fake-3ds/payments/$payment_id/complete" \
  -H 'Content-Type: application/json' \
  -d '{"approved":true}' \
  >/dev/null

order_status=""
for _ in $(seq 1 30); do
  order_response="$(curl -fsS "$GATEWAY_URL/api/orders/$order_id" -H "Authorization: Bearer $customer_token")"
  order_status="$(jq -er '.status' <<<"$order_response")"

  if [[ "$order_status" == "2" ]]; then
    echo "confirmed-order=$order_response"
    exit 0
  fi

  sleep 1
done

echo "Order did not reach Confirmed; status=$order_status order=$order_id payment=$payment_id" >&2
exit 1
