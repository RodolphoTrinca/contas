# Funcionalidades e contrato

[Início](../README.md) · [Arquitetura](arquitetura.md) · [Autenticação](autenticacao.md)

O Banco KRT gerencia o cadastro e o acesso a contas de clientes. Fraude e Cartões demonstram como outros sistemas consultam essas contas e recebem suas alterações.

## Contas e permissões

| Funcionalidade | Comportamento |
|---|---|
| Abrir | Nome com 2 a 120 caracteres após remover espaços nas extremidades. CPF com dígitos verificadores válidos. Conta começa ativa, com UUID e versão 1. |
| Consultar | Lista paginada e consulta por UUID. CPF retornado como `***.***.***-XX`. |
| Editar | Altera nome e status `active` ou `inactive`. CPF permanece imutável. |
| Ativar ou inativar | Usa a mesma operação de edição. Conta inativa continua consultável. |
| Excluir | Exclusão lógica terminal para aquele UUID. A conta sai das consultas usuais e o CPF fica disponível para nova abertura. |
| Atribuir carteira | Operador informa o UUID do gerente. A abertura feita por um gerente já atribui a conta à própria carteira. |
| Vincular titular | Operador informa o UUID do cliente. Uma identidade pode ter uma conta não excluída vinculada. |
| Consultar auditoria | Operador vê alterações de todas as contas, inclusive excluídas, e pode filtrar por UUID. |
| Acompanhar entregas | No modo `demos`, painel separado mostra recebimento, processamento e JSON em cada consumidor. |

O PostgreSQL garante CPF único entre contas não excluídas, inclusive quando duas aberturas ocorrem ao mesmo tempo. Busca por nome e filtros da interface atuam sobre a página carregada. O escopo funcional atual concentra-se em cadastro, permissões e integrações demonstrativas.

## Rotas da API

Todas as rotas abaixo começam com `/api/v1`. O acesso por perfil e carteira é detalhado no [guia de autenticação](autenticacao.md).

| Método e caminho | Uso | Perfil |
|---|---|---|
| `GET /accounts` | Listar com `limit` de 1 a 100 e `offset` a partir de zero. Padrão de 25 itens. | Operador, Gerente, Cliente |
| `GET /accounts/{id}` | Consultar uma conta visível ao usuário | Operador, Gerente, Cliente |
| `POST /accounts` | Abrir conta | Operador, Gerente |
| `PUT /accounts/{id}` | Editar nome e status | Operador, Gerente |
| `DELETE /accounts/{id}` | Excluir logicamente | Operador, Gerente |
| `PUT /accounts/{id}/portfolio` | Atribuir ou transferir carteira | Operador |
| `PUT /accounts/{id}/owner` | Vincular titular | Operador |
| `GET /accounts/history` | Auditoria, com `accountId`, `limit` e cursor `before` | Operador |
| `GET /accounts/permissions` | Permissões da interface e escopo de idempotência | Operador, Gerente, Cliente |
| `GET /fraud/accounts/{id}/status` | Consultar status | Cliente de serviço Fraude |
| `GET /cards/accounts/{id}/eligibility` | Consultar elegibilidade demonstrativa pelo status | Cliente de serviço Cartões |

## Respostas

A API de Contas usa o envelope `data`, `error` e `traceId`. Uma abertura retorna **201** e o cabeçalho `Location`. As demais operações bem-sucedidas retornam **200**. Exemplo de exclusão concluída e de falha de validação.

```json
{
  "data": { "id": "6610534a-8133-4c67-9bdf-169021e0fd81", "deleted": true },
  "error": null,
  "traceId": "identificador-da-requisicao"
}
```

```json
{
  "data": null,
  "error": { "code": "account.invalid_cpf", "message": "Informe um CPF válido." },
  "traceId": "identificador-da-requisicao"
}
```

| HTTP | Exemplos de `error.code` | Significado |
|---|---|---|
| 400 | `account.invalid_cpf`, `account.invalid_name`, `request.invalid` | Entrada inválida |
| 401 | `auth.required` | Autenticação necessária |
| 403 | `auth.forbidden` | Perfil ou cliente sem permissão |
| 404 | `account.not_found` | Conta ausente, excluída ou fora da carteira/identidade |
| 409 | `account.duplicate_cpf`, `account.stale_version`, `account.concurrent_change`, `idempotency.key_conflict` | Conflito com o estado persistido |
| 503 | `service.unavailable` | Dependência temporariamente indisponível |
| 500 | `service.internal_error` | Falha inesperada na API |

Os schemas completos estão no [OpenAPI](openapi.json) e no [Scalar local](http://localhost:8080/scalar/v1). O endpoint `/health` e o histórico das demos têm contratos próprios, descritos nos guias de execução e mensageria.

As descrições do Scalar são geradas pelas rotas da API. Os resumos e exemplos ficam em `Http/Onboarding`, `Http/Fraud` e `Http/Cards`. Os metadados comuns de autenticação, parâmetros e respostas ficam em `Http/Documentation/ApiDocumentation.cs`. Ao alterá-los, gere novamente o JSON versionado pela API.

## Reenvio e concorrência

| Operação | Cabeçalhos obrigatórios | Proteção |
|---|---|---|
| Abrir | `Idempotency-Key` com UUID não vazio | Recibo durável por identidade e chave, válido por 24 horas |
| Editar ou excluir | `Idempotency-Key` e `Expected-Version` | Recibo e validação da versão consultada |
| Transferir carteira ou vincular titular | Autenticação de Operador | Repetir o mesmo vínculo é uma operação sem efeito. EF Core verifica concorrência na gravação. |

Em um retry de abertura, edição ou exclusão, reutilize chave, conteúdo e versão originais. O servidor consulta o recibo antes de comparar a versão atual. O cabeçalho `Idempotency-Replayed` identifica o replay. Reutilizar a chave com outra operação ou conteúdo retorna conflito.

A interface guarda chave, versão e horário em `sessionStorage`, por usuário e impressão SHA-256 da operação. A recarga da mesma aba preserva a tentativa. Armazenamento indisponível ou tentativa expirada bloqueiam a escrita nessa aba. Uma chave diferente representa outra tentativa, sujeita às regras de CPF e versão. O escopo dessas garantias está detalhado na [arquitetura](arquitetura.md).

## Auditoria

A tabela `audit.account_changes` guarda conta, ação, autor e horário UTC. O autor usa `azp:sub` do token validado. O seed usa `system:seed`.

| Ação | Gera auditoria | Gera evento para Fraude e Cartões |
|---|---|---|
| Abertura, edição efetiva e exclusão | Sim | Sim |
| Mudança de carteira ou titular | Sim | Apenas auditoria neste fluxo |
| Consulta, falha, replay ou alteração sem efeito | Sem novo registro | Sem novo evento |

A trilha registra quem executou a ação e quando. Os campos armazenados se limitam aos metadados acima, sem snapshot de valores anteriores e posteriores. Ela cobre as alterações feitas após a aplicação da migração de auditoria.
