# Autenticação e integração

O Next.js usa OIDC Authorization Code com PKCE. O callback verifica o estado da solicitação e troca o código com o Keycloak no servidor. O navegador recebe um cookie HttpOnly. A rota de servidor do Next.js usa o token como Bearer ao chamar a API. A API valida emissor, audiência `contas-api`, assinatura e validade.

O realm local provisiona `operador` / `operador-local` com a role `accounts-operator`. Os clientes confidenciais `fraud-api` e `cards-api` usam client credentials e têm roles próprias. Os valores do realm são exclusivos para o ambiente local.

Para obter um token de Fraude:

```sh
BASE=http://localhost:8080
FRAUD_TOKEN=$(curl -fsS "$BASE/auth/realms/contas/protocol/openid-connect/token" \
  --data grant_type=client_credentials --data client_id=fraud-api \
  --data client_secret=fraud-api-local | jq -r .access_token)
```

Para Cartões, use `client_id=cards-api` e `client_secret=cards-api-local`.

| Cliente | Role | Acesso |
|---|---|---|
| contas-web | accounts-operator | CRUD de contas |
| fraud-api | fraud-reader | Status da conta |
| cards-api | cards-reader | Elegibilidade demonstrativa, baseada no status ativo |

Crie uma conta pela interface e use seu ID:

```sh
curl -i -H "Authorization: Bearer $FRAUD_TOKEN" "$BASE/api/v1/fraud/accounts/$ID/status"
curl -i -H "Authorization: Bearer $FRAUD_TOKEN" "$BASE/api/v1/cards/accounts/$ID/eligibility"
```

A primeira chamada retorna 200 e a segunda 403. O token de Cartões tem o comportamento inverso.

O modo `demos` inicia as duas instâncias do projeto em `samples/Contas.PartnerDemo`. Fraude atende em `http://localhost:5091/api/v1/accounts/$ID` e Cartões em `http://localhost:5092/api/v1/accounts/$ID`. Cada instância usa seu próprio cliente Keycloak para chamar a API de Contas. Essas portas aceitam conexões apenas da máquina local.

A API de demonstração também pode ser iniciada separadamente:

```sh
Partner__ClientId=fraud-api \
Partner__Secret=fraud-api-local \
Partner__TokenEndpoint=http://localhost:8080/auth/realms/contas/protocol/openid-connect/token \
Partner__AccountsApi=http://localhost:8080 \
Partner__ResourcePath='/api/v1/fraud/accounts/{id}/status' \
dotnet run --project samples/Contas.PartnerDemo --urls http://127.0.0.1:5090
```

Chame `curl http://127.0.0.1:5090/api/v1/accounts/$ID`. A demonstração obtém o token no servidor e encaminha a resposta da API de contas.
