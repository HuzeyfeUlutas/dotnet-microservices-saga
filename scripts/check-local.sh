#!/usr/bin/env bash

set -euo pipefail

SOLUTION="${SOLUTION:-MarketplaceOrderPlatform.sln}"
FAIL_ON_VULNERABLE="${FAIL_ON_VULNERABLE:-false}"

require_command() {
  if ! command -v "$1" >/dev/null; then
    echo "Required command was not found: $1" >&2
    exit 1
  fi
}

run_step() {
  local name="$1"
  shift

  echo
  echo "==> $name"
  "$@"
}

require_command dotnet

run_step "Restore" dotnet restore "$SOLUTION"
run_step "Build" dotnet build "$SOLUTION" --no-restore -m:1 -nr:false
run_step "Test" dotnet test "$SOLUTION" --no-build -m:1 -nr:false

echo
echo "==> Vulnerable package check"
vulnerability_output="$(mktemp)"
trap 'rm -f "$vulnerability_output"' EXIT

if dotnet list "$SOLUTION" package --vulnerable --include-transitive | tee "$vulnerability_output"; then
  vulnerability_check_status=0
else
  vulnerability_check_status=$?
fi

if grep -q "has the following vulnerable packages" "$vulnerability_output"; then
  if [[ "$FAIL_ON_VULNERABLE" == "true" ]]; then
    echo "Vulnerable packages were reported and FAIL_ON_VULNERABLE=true." >&2
    exit 1
  fi

  echo "Vulnerable packages were reported. Continuing because FAIL_ON_VULNERABLE=false."
elif [[ "$vulnerability_check_status" -ne 0 ]]; then
  echo "Vulnerable package check command failed with exit code $vulnerability_check_status." >&2
  exit "$vulnerability_check_status"
else
  echo "No vulnerable packages reported."
fi
