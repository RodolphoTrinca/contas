# Contas

Gestão de contas com .NET 10, Next.js, PostgreSQL, Redis, Keycloak e Nginx.

## Começar

```sh
bash scripts/run.sh
```

No PowerShell, use `./scripts/run.ps1`. Escolha o ambiente no menu e, para a stack completa, a stack com demos ou a API, informe se deseja carregar contas fictícias. Abra [localhost:8080](http://localhost:8080/) e entre com `operador` / `operador-local`. A documentação interativa está no [Scalar](http://localhost:8080/scalar/v1).

## Documentação

- [Execução, seed e depuração no VS Code](docs/execucao.md)
- [Arquitetura e padrões do projeto](docs/arquitetura.md)
- [Design responsivo e tema do Keycloak](docs/design.md)
- [Especificação funcional](docs/spec.md)
- [Contrato OpenAPI](docs/openapi.json)
- [Login e integração com Keycloak](docs/autenticacao.md)
- [Testes .NET e Playwright](docs/testes.md)
- [Proposta para AWS](docs/aws.md)

O código está em `src/`, o ambiente local em `infra/`, os comandos em `scripts/`, os exemplos em `samples/` e as suítes em `tests/`. As instruções para agentes ficam em [AGENTS.md](AGENTS.md).
