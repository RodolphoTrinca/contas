# Executar localmente

Instale Docker com Compose v2. Para desenvolver fora dos contêineres, instale também .NET 10 e Node.js 20.9 ou superior.

Execute `bash scripts/run.sh` ou `./scripts/run.ps1` sem argumentos para abrir o menu. O Docker só é acionado após a seleção. Nas duas opções de stack completa e no modo API, o menu também pergunta se deve carregar contas fictícias. Use os argumentos abaixo em automações.

| Modo | Bash | PowerShell |
|---|---|---|
| Stack completa | `bash scripts/run.sh stack` | `./scripts/run.ps1 -Mode stack` |
| Stack com demos de Fraude e Cartões | `bash scripts/run.sh demos` | `./scripts/run.ps1 -Mode demos` |
| Stack com contas fictícias | `bash scripts/run.sh stack --seed` | `./scripts/run.ps1 -Mode stack -Seed` |
| API, Scalar e dependências | `bash scripts/run.sh api --no-seed` | `./scripts/run.ps1 -Mode api` |
| API com contas fictícias | `bash scripts/run.sh api --seed` | `./scripts/run.ps1 -Mode api -Seed` |
| Somente PostgreSQL, Redis e Keycloak | `bash scripts/run.sh infra` | `./scripts/run.ps1 -Mode infra` |
| Dependências e gateway para depuração | `bash scripts/run.sh debug` | `./scripts/run.ps1 -Mode debug` |
| Encerrar preservando dados | `bash scripts/run.sh down` | `./scripts/run.ps1 -Mode down` |

O gateway local usa [localhost:8080](http://localhost:8080/). O Scalar fica em [/scalar/v1](http://localhost:8080/scalar/v1). O usuário de teste do Keycloak é `operador` com senha `operador-local`. A opção de seed cria dados de conta no PostgreSQL. O Keycloak provisiona o usuário e os clientes de teste em ambos os modos. Rodar sem seed preserva registros previamente criados.

O volume do PostgreSQL mantém os bancos da aplicação e do Keycloak. Nos modos `stack`, `demos`, `api` e `debug`, o Compose executa o serviço `migrations` e espera sua saída com código zero antes de iniciar a API ou o gateway de depuração. O modo `infra` apenas inicia as dependências. O seed opcional roda no serviço de migração após a atualização do schema.

Se você roda a API diretamente com as dependências locais já iniciadas, aplique as migrações antes de iniciar o servidor:

```sh
dotnet run --project src/Contas.Api -- --migrate --environment Development
```

Para adicionar uma migração:

```sh
dotnet tool restore
dotnet ef migrations add NomeDaMigracao --project src/Contas.Infrastructure --startup-project src/Contas.Api --output-dir Persistence/Migrations -- --environment Development
```

## VS Code

Inicie o modo `debug`, instale as dependências da interface com `npm ci` em `src/Contas.Web` e abra a raiz do projeto no VS Code. Use **API debug → Scalar** para depurar o backend com o Scalar ou **Stack debug → UI** para iniciar API e Next.js e abrir a interface. Os perfis usam as portas locais padrão.

O gateway de debug usa o endereço da ponte Docker para alcançar os processos iniciados no host. Em ambientes em que o daemon Docker roda em outra máquina ou VM, defina `CONTAS_DEBUG_HOST` com o endereço que o contêiner consegue acessar.
