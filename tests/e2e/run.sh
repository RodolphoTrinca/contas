#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$root"
export ACCOUNTS_PORT="${ACCOUNTS_PORT:-18080}"
export POSTGRES_PORT="${TEST_POSTGRES_PORT:-25432}" REDIS_PORT="${TEST_REDIS_PORT:-26379}"
export KEYCLOAK_PORT="${TEST_KEYCLOAK_PORT:-28090}" ACCOUNTS_SEED=false
export ACCOUNTS_E2E_URL="http://localhost:$ACCOUNTS_PORT"
project="contas-e2e-$$"
compose=(docker compose -p "$project" -f "$root/infra/compose.yaml")
trap '"${compose[@]}" down -v' EXIT
"${compose[@]}" up --build -d nginx
for attempt in {1..90}; do
  if curl --max-time 2 -fsS "$ACCOUNTS_E2E_URL/health" >/dev/null 2>&1 &&
     curl --max-time 2 -fsS "$ACCOUNTS_E2E_URL/auth/realms/contas/.well-known/openid-configuration" >/dev/null 2>&1 &&
     curl --max-time 2 -fsS "$ACCOUNTS_E2E_URL/" >/dev/null 2>&1; then
    if [ "${ACCOUNTS_EXPORT_OPENAPI:-false}" = true ]; then
      curl -fsS "$ACCOUNTS_E2E_URL/openapi/v1.json" -o docs/openapi.json
    fi
    cd tests/e2e
    npm test
    exit
  fi
  sleep 1
done
"${compose[@]}" logs --tail=100
exit 1
