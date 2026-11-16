# Testes

[Início](../README.md) · [Execução](execucao.md) · [Design](design.md)

Os testes verificam regras, persistência real e jornadas pelo navegador. Os scripts criam projetos Compose descartáveis para integração e E2E, com portas e volumes próprios.

## Executar

Instale SDK .NET 10, Docker com Compose e Node.js 20.9 ou superior. Prepare o Playwright uma vez.

```bash
cd tests/e2e
npm ci
npx playwright install chromium --only-shell
```

No Linux, se faltarem bibliotecas de sistema, execute `npx playwright install-deps chromium`. Retorne à raiz para usar os scripts.

| Suíte | Bash | PowerShell |
|---|---|---|
| Domínio e arquitetura | `bash scripts/test.sh fast` | `./scripts/test.ps1 -Mode fast` |
| Integração PostgreSQL/Redis | `bash scripts/test.sh integration` | `./scripts/test.ps1 -Mode integration` |
| Playwright | `bash scripts/test.sh e2e` | `./scripts/test.ps1 -Mode e2e` |
| Todas as anteriores | `bash scripts/test.sh all` | `./scripts/test.ps1 -Mode all` |

Para incluir Floci e o cenário de fan-out, habilite a variável antes da execução.

```bash
ACCOUNTS_TEST_FANOUT=true bash scripts/test.sh all
```

```powershell
$env:ACCOUNTS_TEST_FANOUT = "true"
./scripts/test.ps1 -Mode all
```

Sem essa variável, o teste de fan-out é ignorado. `all` respeita a mesma opção.

## Cobertura .NET

| Suíte | O que verifica |
|---|---|
| Domínio | CPF inválido, normalização, abertura, versão, exclusão terminal, alterações sem efeito, carteira, titular e limites dos value objects |
| Arquitetura | Domain depende do framework, Application depende de Domain e abstrações do Mediator, Infrastructure preserva a direção das referências e o grafo dos projetos segue as camadas |
| Integração | Migrações, oito aberturas concorrentes com a mesma chave, recibo em novo DbContext, conflito de chave, invalidação Redis, payload da outbox, inbox dos dois consumidores, auditoria e paginação |

A integração chama handlers e adaptadores diretamente com PostgreSQL e Redis reais. O contrato HTTP é exercitado pelo Playwright.

## Cenários Playwright

| Arquivo em `tests/e2e` | Cenários |
|---|---|
| `accounts.spec.ts` | CRUD, resposta de criação perdida e reenvio, Scalar/OpenAPI com descrições, parâmetros, cabeçalhos e acesso ao histórico exclusivo do Operador |
| `login.spec.ts` | OIDC, cookie HttpOnly, logout e acesso anônimo |
| `authorization.spec.ts` | Fraude e Cartões acessam sua rota e recebem 403 no acesso cruzado ou ao CRUD |
| `portfolios.spec.ts` | Dois gerentes, atribuição automática, isolamento de carteira, transferência pelo Operador, replay após transferência e consulta do Cliente |
| `responsive.spec.ts` | Uso no celular e ausência de controles de escrita para o Cliente |
| `design-v2.spec.ts` | Fluxos e estados visuais em claro/escuro, larguras 1440, 768, 390 e 320 px, foco e ausência de rolagem horizontal |
| `theme.spec.ts` | Preferência do sistema, persistência, sincronização entre abas, armazenamento indisponível e continuidade pelo Keycloak |
| `pending-write.unit.spec.ts` | Chave e versão preservadas após recarga, expiração e falhas ambíguas, em testes da lógica frontend |
| `fanout.spec.ts` | Criação real, mesmo evento nos dois consumidores, deduplicação, JSON, polling de 15 segundos e painel mobile em tema escuro |

Os testes de negócio usam a stack real. O teste visual intercepta alguns dados no navegador para reproduzir estados determinísticos. As capturas servem para revisão visual. As asserções cobrem layout e comportamento, com comparação pixel a pixel fora da cobertura atual.

Para listar os casos disponíveis, execute `npx playwright test --list` dentro de `tests/e2e`. Isso evita depender de uma contagem fixa na documentação.

## Relatórios e contrato

Para gerar um relatório HTML, rode contra uma stack descartável já iniciada, a partir de `tests/e2e`. Ajuste a URL à porta dessa stack.

```bash
ACCOUNTS_E2E_URL=http://localhost:18080 npm test -- --reporter=html
npx playwright show-report
```

Para incluir o cenário de mensageria nessa execução manual, configure também `ACCOUNTS_HISTORY_URL` e `ACCOUNTS_FANOUT_PROJECT` para o painel e o nome do projeto Compose. O script `scripts/test.sh e2e` já define essas variáveis no modo fan-out.

A especificação versionada pode ser atualizada a partir da API local. Revise o diff antes de commitá-la.

```bash
curl -fsS http://localhost:8080/openapi/v1.json -o docs/openapi.json
```

Resultados e capturas ficam em diretórios ignorados pelo Git. Os testes de backend são .NET/xUnit. O Python existente provisiona o Floci. [Fluxo de mensageria](mensageria.md)
