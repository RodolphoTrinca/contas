#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
usage() { echo "Uso: bash scripts/run.sh {stack|demos|api|infra|debug|down} [--seed|--no-seed]" >&2; }
if (( $# > 2 )); then usage; exit 2; fi
if (( $# == 0 )); then
  if [[ ! -t 0 ]]; then usage; exit 2; fi
  echo "Qual ambiente deseja executar?"
  echo "1) Stack completa (UI, API e infraestrutura)"
  echo "2) Somente API e infraestrutura"
  echo "3) Somente infraestrutura (PostgreSQL, Redis e Keycloak)"
  echo "4) Infraestrutura para depuração no VS Code"
  echo "5) Encerrar os serviços"
  echo "6) Stack completa com demos de Fraude e Cartões"
  while true; do
    read -r -p "Escolha [1-6] ou q para cancelar: " choice || exit 1
    case "$choice" in
      1) mode=stack; break ;;
      2) mode=api; break ;;
      3) mode=infra; break ;;
      4) mode=debug; break ;;
      5) mode=down; break ;;
      6) mode=demos; break ;;
      q|Q) exit 0 ;;
      *) echo "Opção inválida." ;;
    esac
  done
  if [[ "$mode" == stack || "$mode" == api || "$mode" == demos ]]; then
    while true; do
      read -r -p "Carregar contas fictícias? [s/N]: " answer || exit 1
      case "$answer" in
        s|S) set -- "$mode" --seed; break ;;
        ''|n|N) break ;;
        *) echo "Responda s ou n." ;;
      esac
    done
  fi
else
  mode="$1"
fi
seed="${2:---no-seed}"
case "$seed" in
  --seed) export ACCOUNTS_SEED=true ;;
  --no-seed) export ACCOUNTS_SEED=false ;;
  *) echo "Uso: bash scripts/run.sh {stack|demos|api|infra|debug|down} [--seed|--no-seed]" >&2; exit 2 ;;
esac
compose=(docker compose -f "$root/infra/compose.yaml")
fanout="$root/infra/compose.fanout.yaml"
case "$mode" in
  stack|api|infra|debug) "${compose[@]}" -f "$fanout" --profile demos stop message-history fraud-demo cards-demo floci ;;
esac
case "$mode" in
  demos) "${compose[@]}" --profile api -f "$root/infra/compose.debug.yaml" stop api-gateway debug-gateway; "${compose[@]}" -f "$fanout" --profile demos up --build -d nginx message-history ;;
  stack) "${compose[@]}" --profile api -f "$root/infra/compose.debug.yaml" stop api-gateway debug-gateway; "${compose[@]}" up --build -d nginx ;;
  api) "${compose[@]}" -f "$root/infra/compose.debug.yaml" stop nginx web debug-gateway; "${compose[@]}" --profile api up --build -d api-gateway ;;
  infra) "${compose[@]}" up -d postgres redis keycloak ;;
  debug) "${compose[@]}" --profile api stop nginx api-gateway api web; "${compose[@]}" -f "$root/infra/compose.debug.yaml" up --build -d debug-gateway ;;
  down) "${compose[@]}" -f "$fanout" --profile api --profile demos down --remove-orphans ;;
  *) echo "Uso: bash scripts/run.sh {stack|demos|api|infra|debug|down} [--seed|--no-seed]" >&2; exit 2 ;;
esac
