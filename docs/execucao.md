# Execução local

[Início](../README.md) · [Autenticação](autenticacao.md) · [Testes](testes.md)

Para rodar tudo em contêineres, instale Docker com Compose v2. Para depurar ou executar testes no host, use também SDK .NET 10 e Node.js 20.9 ou superior.

## Escolher os serviços

Execute na raiz `bash scripts/run.sh` ou `./scripts/run.ps1`. O menu coleta a escolha antes de iniciar os serviços. Os modos `stack`, `demos` e `api` também perguntam sobre seed.

| Modo | O que inicia | Bash | PowerShell |
|---|---|---|---|
| `stack` | UI, API, banco, cache, identidade e Nginx | `bash scripts/run.sh stack` | `./scripts/run.ps1 -Mode stack` |
| `demos` | Stack completa, Floci, demos e painel | `bash scripts/run.sh demos` | `./scripts/run.ps1 -Mode demos` |
| `api` | API, Scalar e dependências | `bash scripts/run.sh api` | `./scripts/run.ps1 -Mode api` |
| `infra` | PostgreSQL, Redis e Keycloak | `bash scripts/run.sh infra` | `./scripts/run.ps1 -Mode infra` |
| `debug` | Dependências, migrações e gateway para o host | `bash scripts/run.sh debug` | `./scripts/run.ps1 -Mode debug` |
| `down` | Encerra serviços e preserva volumes | `bash scripts/run.sh down` | `./scripts/run.ps1 -Mode down` |

Os comandos com modo explícito começam sem seed. Para habilitar, acrescente `--seed` no Bash ou `-Seed` no PowerShell. No Bash, `--no-seed` também explicita a escolha.

## Endereços

| Serviço | Endereço padrão |
|---|---|
| Aplicação | [localhost:8080](http://localhost:8080/) |
| Scalar / OpenAPI | [Scalar](http://localhost:8080/scalar/v1) · [JSON](http://localhost:8080/openapi/v1.json) |
| Keycloak administrativo | [Console](http://localhost:8080/auth/admin/) |
| Demo Fraude / Cartões | `localhost:5091` / `localhost:5092` |
| Painel de entregas | [localhost:5093](http://localhost:5093/) |
| PostgreSQL / Redis no host | `127.0.0.1:15432` / `127.0.0.1:16379` |
| Keycloak direto | `http://localhost:18090/auth` |

`GET /health` retorna 200 quando o servidor HTTP atende. Essa rota verifica o processo HTTP. Para conferir dependências e funcionalidades, use os [testes de integração e E2E](testes.md).

## Dados e migrações

O seed cria **Conta Demonstração**, com CPF fictício `12345678909`, usando o caso de uso de abertura. As carteiras e o titular dessa conta começam sem vínculo. Use os perfis e UUIDs do [guia de autenticação](autenticacao.md) para preparar os cenários de acesso.

Os usuários e clientes Keycloak são importados ao criar o realm, independentemente do seed. O import preserva realms existentes. Executar sem seed mantém os dados já gravados.

Os modos `stack`, `demos`, `api` e `debug` aguardam o serviço `migrations` concluir. Ele aplica migrações EF Core e depois o seed opcional. O modo `infra` apenas inicia as dependências. PostgreSQL, Floci e os históricos SQLite usam volumes persistentes.

Para migrar manualmente com as dependências locais iniciadas, execute na raiz.

```bash
dotnet run --project src/Contas.Api -- --migrate --environment Development
```

Para criar uma migração durante o desenvolvimento.

```bash
dotnet tool restore
dotnet ef migrations add NomeDaMigracao --project src/Contas.Infrastructure --startup-project src/Contas.Api --output-dir Persistence/Migrations -- --environment Development
```

Revise o código gerado antes de aplicar. O fluxo de release está no [guia AWS](aws.md).

## Depurar no VS Code

1. Inicie `bash scripts/run.sh debug` ou `./scripts/run.ps1 -Mode debug`.
2. Execute `npm ci` dentro de `src/Contas.Web`.
3. Abra a raiz no VS Code com a extensão de depuração C# instalada.
4. Escolha o perfil adequado e inicie com F5.

| Perfil | Resultado |
|---|---|
| **API debug → Scalar** | API no host, com abertura do Scalar pelo gateway |
| **API debug para UI** | API no host para uso em conjunto com o frontend |
| **Next.js debug → UI** | Next.js no host, com abertura da aplicação |
| **Stack debug → UI** | API e Next.js iniciados em conjunto |

O `debug-gateway` encaminha chamadas aos processos do host nas portas 5080 e 3000. Se o daemon Docker estiver em outra VM ou máquina, defina `CONTAS_DEBUG_HOST` com o endereço alcançável pelo contêiner.

## Diagnóstico rápido

| Sintoma | Conferir |
|---|---|
| Porta ocupada | Variáveis `ACCOUNTS_PORT`, `POSTGRES_PORT`, `REDIS_PORT`, `KEYCLOAK_PORT`, `FRAUD_DEMO_PORT`, `CARDS_DEMO_PORT` e `MESSAGE_HISTORY_PORT` |
| API falha após mudar schema | Saída do serviço `migrations` |
| Novo usuário ausente | O realm existente é preservado. Confira usuários e roles no Keycloak. |
| Painel sem eventos | Modo `demos`, filas, logs dos consumidores e criação de uma nova conta |
| Gateway de debug retorna 502 | Processos API/Next.js iniciados e endereço `CONTAS_DEBUG_HOST` |

Para consultar a stack de demos.

```bash
docker compose -f infra/compose.yaml -f infra/compose.fanout.yaml --profile demos ps
docker compose -f infra/compose.yaml -f infra/compose.fanout.yaml --profile demos logs --tail=80 api fraud-demo cards-demo
```

O passo a passo para conferir a mesma mensagem nos dois consumidores está em [mensageria](mensageria.md).
