# Testes

| Suíte | Comando Bash | O que cobre |
|---|---|---|
| Domínio e arquitetura | `bash scripts/test.sh fast` | Validação de CPF, ciclo de vida da conta, eventos do agregado e direção das referências entre projetos. |
| Integração .NET | `bash scripts/test.sh integration` | Migração, PostgreSQL e Redis reais, oito tentativas concorrentes, recibo durável, conflito de chave e invalidação. |
| Navegador | `bash scripts/test.sh e2e` | Interface Next.js, login Keycloak, Nginx, contrato HTTP e fronteiras entre clientes. |

O smoke em Python surgiu no primeiro MVP porque a biblioteca padrão permitia verificar HTTP e SQLite sem instalar ferramentas. Esta versão usa xUnit para as verificações de backend e Playwright para a jornada no navegador. Os scripts de teste criam projetos Compose e volumes descartáveis separados do ambiente de desenvolvimento.

## Instalar o Playwright

```sh
cd tests/e2e
npm ci
npx playwright install chromium --only-shell
```

Se faltarem bibliotecas de sistema no Linux, execute `npx playwright install-deps chromium`. A partir da raiz, rode `bash scripts/test.sh e2e`.

No PowerShell, rode `./scripts/test.ps1 -Mode fast`, `-Mode integration`, `-Mode e2e` ou `-Mode all`.

## Cenários no Chromium

- Login OIDC com cookie HttpOnly, logout e acesso anônimo.
- Abertura de conta, perda da primeira resposta depois de o servidor gravar a conta, reenvio com a mesma chave, edição e exclusão.
- Credenciais de serviço de Fraude e Cartões, respostas 200 na rota autorizada e 403 no acesso cruzado ou ao CRUD.
- Contrato de erro para falta de autenticação e CPF inválido.
- Scalar e OpenAPI servidos pelo Nginx, com a chave de idempotência documentada.

Para atualizar a especificação a partir do ambiente padrão, use `curl -fsS http://localhost:8080/openapi/v1.json -o docs/openapi.json`.

O cenário de carteiras usa dois gerentes, um cliente e o Operador autenticados pelo Keycloak. Ele verifica abertura com atribuição automática, consultas e alterações dentro e fora da carteira, transferência exclusiva do Operador, perda de acesso após transferência, reenvio de uma abertura anterior e consulta do titular.

## Interface v2

`design-v2.spec.ts` verifica listagem, busca e filtros da página, abertura, CPF inválido, edição de status, carteira, titular, exclusão, lista vazia, indisponibilidade, sessão expirada e acesso negado em 1440, 768, 390 e 320 px. Verifica também que os painéis cabem na tela, não há rolagem horizontal e o foco retorna ao botão de origem após fechar.

Cada etapa anexa uma captura de tela aos resultados. A suíte inclui login inválido, mostrar senha e confirmação de saída no Keycloak real. Para gerar um relatório navegável, use `npm test -- --reporter=html` em `tests/e2e` com `ACCOUNTS_E2E_URL` apontando para uma stack descartável. Consulte [o mapa das referências v2](design.md) para comparar os componentes com o ZIP.
