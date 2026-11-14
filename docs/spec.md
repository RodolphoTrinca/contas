# Especificação do MVP de contas

| Operação | Critério de aceitação |
|---|---|
| Abrir | CPF válido e único entre contas não excluídas. Conta inicia ativa. Retorna 201 e Location. |
| Listar | Paginação por limit e offset. CPF mascarado e contas excluídas omitidas. |
| Consultar | Retorna a conta por ID, mesmo inativa. ID ausente ou excluído retorna 404. |
| Alterar | Aceita novo nome e status. CPF é imutável. Alteração efetiva incrementa a versão e registra um evento. |
| Excluir | Exclusão lógica e terminal para o ID. Retorna 200 e libera o CPF para nova abertura. |
| Reenviar | POST, PUT e DELETE exigem UUID em Idempotency-Key. Reenvio do mesmo conteúdo em até 24h repete a resposta de sucesso. Reuso com outro conteúdo ou operação retorna 409. |
| Comunicar | Alteração efetiva e evento persistem na mesma transação. Fraude e Cartões recebem uma cópia cada no ambiente local. |
| Consultar novamente | Cache individual até a próxima meia-noite de São Paulo. Alteração e exclusão invalidam a entrada após o commit. |

O contrato HTTP está em [OpenAPI](openapi.json). O [guia AWS](aws.md) descreve a evolução do perfil local para PostgreSQL, cache distribuído e SNS/SQS. O perfil local usa Keycloak e dados fictícios.

A suíte unitária cobre as invariantes do agregado. O teste integrado cobre HTTP, invalidação, escrita concorrente, reenvio após reinício e entrega aos destinatários. O Playwright cobre a jornada real pelo Nginx, incluindo a perda da primeira resposta depois de o servidor confirmar a criação. O segundo envio reutiliza a mesma chave e a lista mostra uma única conta.

## Contrato HTTP v1

Os endpoints de negócio usam `/api/v1`. O sucesso retorna `data`, `error: null` e `traceId`. A falha retorna `data: null`, `error.code`, `error.message` e `traceId`. A exclusão retorna 200 com `{ "id": "...", "deleted": true }` em `data`. O cabeçalho `Idempotency-Replayed` identifica reenvios bem-sucedidos. Cada requisição tem seu próprio identificador de rastreamento. Consulte [as permissões e os exemplos de integração](autenticacao.md).

## Acesso às contas

- Operador consulta e gerencia todas as contas. Somente esse perfil pode atribuir uma carteira, transferir uma conta ou vincular o titular.
- Gerente consulta, altera e exclui somente contas da sua carteira. Uma conta aberta pelo gerente é atribuída a ele na criação.
- Cliente consulta somente a conta vinculada a sua identidade. A API bloqueia operações de escrita.
- Consultas e alterações por ID fora do escopo retornam 404. Após uma transferência, a API verifica novamente a carteira antes de devolver um recibo de idempotência.
