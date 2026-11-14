#!/bin/sh
set -eu
if [ -z "${CONTAS_DEBUG_HOST:-}" ]; then
    CONTAS_DEBUG_HOST="$(ip route | awk '/^default/ {print $3; exit}')"
fi
export CONTAS_DEBUG_HOST
envsubst '${CONTAS_DEBUG_HOST}' </opt/contas/debug.conf.template >/etc/nginx/conf.d/default.conf
exec nginx -g 'daemon off;'
