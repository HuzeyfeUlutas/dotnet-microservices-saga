#!/usr/bin/env bash

set -euo pipefail

CATALOG_URL="${CATALOG_URL:-http://localhost:8080}"
INVENTORY_URL="${INVENTORY_URL:-http://localhost:8081}"
PAYMENT_URL="${PAYMENT_URL:-http://localhost:8083}"
ORDER_URL="${ORDER_URL:-http://localhost:8084}"

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

require_command curl
require_command jq
require_command seq
require_command uuidgen

wait_ready "$CATALOG_URL"
wait_ready "$INVENTORY_URL"
wait_ready "$PAYMENT_URL"
wait_ready "$ORDER_URL"

suffix="$(uuid)"
sku="SMOKE-$suffix"
buyer_id="$(uuid)"

brand_id="$(
  curl -fsS -X POST "$CATALOG_URL/api/brands" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg name "Smoke Brand $suffix" '{name:$name,description:"compose smoke"}')" |
    jq -er '.id'
)"
category_id="$(
  curl -fsS -X POST "$CATALOG_URL/api/categories" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg name "Smoke Category $suffix" '{name:$name,description:"compose smoke",parentCategoryId:null}')" |
    jq -er '.id'
)"
product_id="$(
  curl -fsS -X POST "$CATALOG_URL/api/products" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg name "Smoke Product $suffix" --arg brand "$brand_id" --arg category "$category_id" '{name:$name,description:"compose smoke",price:199.90,brandId:$brand,categoryId:$category}')" |
    jq -er '.id'
)"
variant_id="$(
  curl -fsS -X POST "$CATALOG_URL/api/products/$product_id/variants" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg sku "$sku" '{name:"Default",sku:$sku}')" |
    jq -er '.id'
)"

curl -fsS -X PUT "$CATALOG_URL/api/products/$product_id" \
  -H 'Content-Type: application/json' \
  -d "$(jq -nc --arg name "Smoke Product $suffix" --arg brand "$brand_id" --arg category "$category_id" '{name:$name,description:"compose smoke",price:199.90,brandId:$brand,categoryId:$category,status:2}')" \
  >/dev/null
curl -fsS -X POST "$CATALOG_URL/api/products/$product_id/variants/$variant_id/activate" >/dev/null

inventory_id="$(
  curl -fsS -X POST "$INVENTORY_URL/api/inventory-items" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg product "$product_id" --arg sku "$sku" '{productId:$product,sku:$sku,initialQuantity:5}')" |
    jq -er '.id'
)"
checkout="$(
  curl -fsS -X POST "$ORDER_URL/api/checkout/pay" \
    -H 'Content-Type: application/json' \
    -d "$(jq -nc --arg buyer "$buyer_id" --arg product "$product_id" --arg sku "$sku" --arg idem "checkout-$suffix" '{buyerId:$buyer,items:[{productId:$product,sku:$sku,quantity:2}],idempotencyKey:$idem,provider:"Fake",method:"Card"}')"
)"
order_id="$(jq -er '.orderId' <<<"$checkout")"
payment_id="$(jq -er '.payment.paymentId' <<<"$checkout")"

echo "product=$product_id"
echo "variant=$variant_id"
echo "inventory=$inventory_id"
echo "order=$order_id"
echo "payment=$payment_id"
echo "action=$(jq -er '.payment.action.type' <<<"$checkout")"
echo "redirect=$(jq -er '.payment.action.redirectUrl' <<<"$checkout")"

curl -fsS -X POST "$PAYMENT_URL/fake-3ds/payments/$payment_id/complete" \
  -H 'Content-Type: application/json' \
  -d '{"approved":true}' \
  >/dev/null

order_status=""
for _ in $(seq 1 30); do
  order_response="$(curl -fsS "$ORDER_URL/api/orders/$order_id")"
  order_status="$(jq -er '.status' <<<"$order_response")"

  if [[ "$order_status" == "2" ]]; then
    echo "confirmed-order=$order_response"
    exit 0
  fi

  sleep 1
done

echo "Order did not reach Confirmed; status=$order_status order=$order_id payment=$payment_id" >&2
exit 1
