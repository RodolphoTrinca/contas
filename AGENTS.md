# Trabalhar no Banco KRT

Leia o [README](README.md) e o guia da área antes de alterar o projeto.

| Área | Referência |
|---|---|
| Regras e contrato HTTP | [Especificação](docs/spec.md) |
| Camadas e domínio | [Arquitetura](docs/arquitetura.md) |
| Bibliotecas e configuração | [Dependências](docs/dependencias.md) |
| Identidade e autorização | [Autenticação](docs/autenticacao.md) |
| Outbox e consumidores | [Mensageria](docs/mensageria.md) |
| Interface e Keycloak | [Design](docs/design.md) |
| Execução e validação | [Execução](docs/execucao.md) · [Testes](docs/testes.md) |

## Organização

- `src/` contém a aplicação, `samples/` as demos e `infra/` o ambiente local.
- Organize casos de uso por operação e cada tipo C# em seu próprio arquivo.
- Preserve as dependências da Clean Architecture. Domain usa o framework, Application depende de Domain e das abstrações do Mediator, Infrastructure implementa as portas e Api compõe o host.
- Regras do agregado e falhas esperadas retornam `Result`. Capture falhas de I/O na fronteira apropriada.

## Contratos e dados

- Preserve API v1, códigos de erro, permissões por endpoint e gravação transacional da conta, recibo e outbox. Atualize OpenAPI e testes quando o contrato mudar.
- Use dados fictícios. Proteja CPF, tokens, senhas e corpos de requisição contra exposição em logs.
- Preserve os volumes de desenvolvimento. A exclusão de volumes se limita a projetos Compose descartáveis de testes.
- Defina versões NuGet em `Directory.Packages.props`, regenere os lockfiles e valide com `dotnet restore --locked-mode`.

## Validação

Execute `bash scripts/test.sh fast` e `bash scripts/test.sh integration` para domínio, arquitetura e persistência. Para alterações de interface, login, proxy ou contrato HTTP, execute também Playwright conforme o guia de testes. Alterações de mensageria devem incluir o cenário com `ACCOUNTS_TEST_FANOUT=true`.

Ao revisar documentação, confira links relativos, comandos, rotas e o comportamento descrito contra os arquivos correspondentes. Diferencie a implementação atual da proposta AWS.
