#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"
mode="${1:-fast}"
case "$mode" in
  fast) dotnet test tests/Domain && dotnet test tests/Architecture ;;
  integration)
    export POSTGRES_PORT="${TEST_POSTGRES_PORT:-25432}" REDIS_PORT="${TEST_REDIS_PORT:-26379}"
    compose=(docker compose -p "contas-integration-$$" -f infra/compose.yaml)
    trap '"${compose[@]}" down -v' EXIT
    "${compose[@]}" up -d --wait postgres redis
    export CONTAS_TEST_POSTGRES="Host=127.0.0.1;Port=$POSTGRES_PORT;Database=contas;Username=contas;Password=contas-local"
    export CONTAS_TEST_REDIS="127.0.0.1:$REDIS_PORT"
    dotnet test tests/Integration ;;
  e2e) bash tests/e2e/run.sh ;;
  all) bash scripts/test.sh fast && bash scripts/test.sh integration && bash scripts/test.sh e2e ;;
  *) echo "Uso: bash scripts/test.sh {fast|integration|e2e|all}" >&2; exit 2 ;;
esac
