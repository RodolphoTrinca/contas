# Banco KRT · Gestão de contas

Aplicação demonstrativa com contas de clientes, carteiras de gerentes, auditoria e integração com Fraude e Cartões. Backend em .NET 10 e interface Next.js responsiva, com temas claro e escuro.

## Rodar localmente

Com Docker e Compose v2 instalados, execute na raiz do projeto.

```bash
bash scripts/run.sh
```

No PowerShell, execute `./scripts/run.ps1`. O menu pergunta quais serviços iniciar e se deseja carregar a conta fictícia. Escolha **Stack completa com demos** para experimentar também o fan-out de eventos.

| Acesso | Endereço |
|---|---|
| Aplicação | [localhost:8080](http://localhost:8080/) |
| Documentação interativa da API | [Scalar](http://localhost:8080/scalar/v1) |
| Mensagens de Fraude e Cartões, no modo `demos` | [localhost:5093](http://localhost:5093/) |

Entre com `operador` / `operador-local`. As credenciais versionadas são de demonstração local. Os demais perfis estão no [guia de autenticação](docs/autenticacao.md).

## O que experimentar

- Abrir, consultar, editar, ativar, inativar e excluir contas.
- Comparar o acesso de Operador, Gerente e Cliente.
- Transferir contas entre carteiras e vincular a identidade do titular.
- Consultar quem alterou cada conta e quando.
- Acompanhar o mesmo evento em Fraude e Cartões, com processamento e JSON da mensagem.

## Documentação

| Quero entender… | Guia |
|---|---|
| Funcionalidades, regras e contrato HTTP | [Especificação](docs/spec.md) |
| Como executar, usar seed e depurar | [Execução local](docs/execucao.md) |
| Camadas, domínio, CQRS e consistência | [Arquitetura](docs/arquitetura.md) |
| Serviços, bibliotecas e motivos das escolhas | [Dependências](docs/dependencias.md) |
| Usuários, permissões e chamadas externas | [Autenticação](docs/autenticacao.md) |
| Outbox, SNS/SQS e painel de entregas | [Mensageria](docs/mensageria.md) |
| Telas, componentes e temas | [Design](docs/design.md) |
| Como testar e quais cenários são cobertos | [Testes](docs/testes.md) |
| Como evoluir para produção | [Proposta AWS](docs/aws.md) |
| Schemas e operações da API | [OpenAPI 3.1.1](docs/openapi.json) |

`src/` contém a aplicação, `samples/` as APIs demonstrativas, `infra/` os contêineres, `scripts/` os comandos e `tests/` as suítes. As orientações para agentes estão em [AGENTS.md](AGENTS.md).
