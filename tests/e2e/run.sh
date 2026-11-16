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
targets=(nginx)
if [ "${ACCOUNTS_TEST_FANOUT:-false}" = true ]; then
  export FRAUD_DEMO_PORT="${TEST_FRAUD_DEMO_PORT:-25091}" CARDS_DEMO_PORT="${TEST_CARDS_DEMO_PORT:-25092}"
  export MESSAGE_HISTORY_PORT="${TEST_MESSAGE_HISTORY_PORT:-25093}"
  export ACCOUNTS_FANOUT_PROJECT="$project"
  export ACCOUNTS_HISTORY_URL="http://localhost:$MESSAGE_HISTORY_PORT"
  compose+=(-f "$root/infra/compose.fanout.yaml" --profile demos)
  targets+=(message-history)
fi
trap '"${compose[@]}" down -v' EXIT
"${compose[@]}" up --build -d "${targets[@]}"
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
