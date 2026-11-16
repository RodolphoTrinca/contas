# Serviços e dependências

[Início](../README.md) · [Arquitetura](arquitetura.md) · [Execução](execucao.md)

Cada serviço atende a uma responsabilidade. O Compose monta o ambiente local e os manifests de pacotes registram as versões utilizadas.

## Serviços locais

| Serviço no Compose | Papel | Motivo da escolha |
|---|---|---|
| `api` | Backend ASP.NET Core com Minimal APIs | .NET 10 reúne HTTP, DI e configuração. Endpoints pequenos delegam regras aos casos de uso. |
| `web` | Next.js 16, React 19 e TypeScript | Interface responsiva e rotas de servidor para OIDC e chamadas à API no mesmo projeto. |
| `postgres` | PostgreSQL 18, com bancos da aplicação e identidade | Transações, índices únicos, concorrência e auditoria durável. Mantém a tecnologia prevista para produção. |
| `redis` | Redis 8 | Cache compartilhado de contas, expiração e operações atômicas de invalidação. |
| `keycloak` | Keycloak 26, realm `contas` | OIDC, usuários, roles e clientes de serviço configuráveis. |
| `nginx` / `api-gateway` | Entrada HTTP na porta 8080 | Encaminha UI, API e autenticação por uma origem comum. O segundo atende ao modo API. |
| `debug-gateway` | Nginx para processos no host | Permite depurar API e Next.js pelo VS Code mantendo a origem usada no login. |
| `migrations` | Processo .NET que aplica schema e seed opcional | Executa antes do servidor e informa sucesso ou falha ao Compose. |
| `floci` | Emulador local de SNS e SQS | Exercita o protocolo dos SDKs AWS com filas e tópicos locais. |
| `messaging-init` | Provisionador do tópico, filas, assinaturas e DLQs | Reproduz a topologia de fan-out ao iniciar as demos. |
| `fraud-demo` / `cards-demo` | Duas instâncias da API de exemplo | Cada instância tem cliente Keycloak, fila e histórico próprios. |
| `message-history` | Nginx na porta 5093 | Serve o painel Next.js e encaminha consultas aos dois históricos. |

O padrão de Minimal APIs reduz configuração e código de transporte. A Microsoft o recomenda para novos projetos de API HTTP. [Documentação ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/apis?view=aspnetcore-10.0)

## Bibliotecas

| Dependência | Onde é usada | Por que foi escolhida |
|---|---|---|
| `Mediator.Abstractions` e `Mediator.SourceGenerator` | Application e API | Commands e queries com contratos tipados e registro de handlers gerado na compilação. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infrastructure | Provedor EF Core para PostgreSQL, com mapeamento, LINQ, transações e concorrência. |
| `Microsoft.EntityFrameworkCore.Design` e `dotnet-ef` | Ferramentas de desenvolvimento | Geram e inspecionam migrações do schema. |
| `StackExchange.Redis` | Infrastructure | Conexão multiplexada reutilizada como singleton e execução de scripts atômicos no Redis. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API | Valida assinatura, emissor, audiência e validade dos tokens. |
| `Microsoft.AspNetCore.OpenApi` e `Scalar.AspNetCore` | API | Geram o contrato HTTP e oferecem navegação e experimentação das operações. |
| `Serilog.AspNetCore` | API e demos | Logs estruturados de requisições e falhas, configurados por ambiente. |
| `Microsoft.Extensions.Hosting.Abstractions` | Infrastructure | Permite implementar o worker como `BackgroundService`. |
| `AWSSDK.SimpleNotificationService` e `AWSSDK.SQS` | Publicador e demos | Publicação e consumo com os SDKs usados também nos serviços AWS. |
| `Microsoft.Data.Sqlite` | Demos | Histórico local persistente por consumidor, com SQL parametrizado e unicidade por evento. |
| Next.js, React e TypeScript | Web | Rotas, componentes e contratos tipados da interface. |
| xUnit, runner e `Microsoft.NET.Test.Sdk` | Testes .NET | Regras de domínio, arquitetura e integração executadas por `dotnet test`. |
| `@playwright/test` | Testes do frontend | Jornadas no Chromium, testes de layout e simulação de falhas de rede. |
| Python e boto3 da imagem Floci | `messaging-init` | Provisionamento do emulador pelo SDK já disponível na imagem. |

O EF Core acessa o PostgreSQL da aplicação. As demos usam SQL direto no SQLite para registrar o recebimento e o processamento. O Python participa somente do provisionamento local de mensageria.

## Versões reproduzíveis

| Arquivo | Conteúdo |
|---|---|
| `Directory.Build.props` | Target `net10.0`, nullable e geração de lockfiles |
| `Directory.Packages.props` | Versões diretas centralizadas dos pacotes NuGet |
| `packages.lock.json` de cada projeto | Dependências NuGet resolvidas, incluindo transitivas |
| `dotnet-tools.json` | Versão local da ferramenta `dotnet-ef` |
| `package.json` e `package-lock.json` | Dependências npm declaradas e versões resolvidas |
| `infra/compose*.yaml` | Imagens dos serviços. Floci está fixado por digest. |

Use `dotnet restore --locked-mode` e `npm ci` para conferir os grafos versionados. Tags de contêiner como `redis:8-alpine` e `nginx:stable-alpine` podem mudar no registro. A proposta de produção fixa imagens por digest. [Gerenciamento central do NuGet](https://learn.microsoft.com/nuget/consume-packages/central-package-management)

## Configuração

O .NET agrupa configurações em `IOptions<T>` por responsabilidade. Variáveis como `Redis__Connection` sobrescrevem a seção correspondente do `appsettings`. No Next.js, `lib/server/server-options.ts` centraliza e tipa o acesso às variáveis do processo. [Options pattern](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0)

| Grupo | Valores principais | Efeito |
|---|---|---|
| `ConnectionStrings` | `Accounts` | Conexão PostgreSQL |
| `Redis` | `Connection` | Conexão e opções do cliente Redis |
| `Auth` | `Issuer`, `Metadata`, `RequireHttps` | Emissor e validação JWT |
| `Seed` | `Enabled` | Carga fictícia no processo de migração |
| `Serilog` | Níveis, filtros e sinks | Configuração dos logs |
| `Messaging` na API | `TopicArn`, `Endpoint`, `Region` | Destino SNS. Sem tópico, entrega na inbox local PostgreSQL. |
| `Messaging` nas demos | `QueueUrl`, `Endpoint`, `Region`, `Consumer`, `DatabasePath` | Fila e histórico SQLite de cada consumidor |
| `Partner` nas demos | `ClientId`, `Secret`, `TokenEndpoint`, `AccountsApi`, `ResourcePath` | Consulta HTTP autenticada à API de Contas |
| Next.js | `APP_ORIGIN`, `OIDC_ISSUER`, `OIDC_INTERNAL`, `API_INTERNAL` | Endereço público e destinos internos do servidor web |

Os valores locais estão no Compose, nos `appsettings.Development.json` e no realm de teste. A configuração de produção é descrita na [proposta AWS](aws.md).
