#!/bin/bash
set -euo pipefail
mkdir -p /tmp/contas-realm
sed "s|http://localhost:\\*|http://localhost:${ACCOUNTS_PORT:-8080}|g" /opt/keycloak/data/import/contas-realm.json > /tmp/contas-realm/contas-realm.json
exec /opt/keycloak/bin/kc.sh start-dev -Dkeycloak.migration.action=import \
    -Dkeycloak.migration.provider=dir -Dkeycloak.migration.dir=/tmp/contas-realm \
    -Dkeycloak.migration.strategy=IGNORE_EXISTING
