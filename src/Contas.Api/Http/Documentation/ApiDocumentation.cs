using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Contas.Api.Http;

internal static class ApiDocumentation
{
    internal static Task DescribeDocument(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken ct)
    {
        document.Info.Title = "Banco KRT · API de Contas";
        document.Info.Description = """
            Gestão de contas, carteiras e titulares, com auditoria e consultas para Fraude e Cartões.
            As operações são agrupadas por contexto e usam o prefixo /api/v1.

            ## Autenticação

            Use um access token do realm contas no campo Bearer de autenticação do Scalar.
            A audiência esperada é contas-api. O cliente contas-web usa login OIDC com PKCE.
            Operador administra todas as contas, Gerente administra a própria carteira e Cliente consulta sua conta vinculada.
            Fraude e Cartões usam client credentials e só acessam as operações de sua integração.

            ## Contrato e reenvio

            Sucesso retorna data, error=null e traceId. Falha retorna data=null, error.code, error.message e traceId.
            Abertura, edição e exclusão exigem Idempotency-Key. Edição e exclusão também exigem Expected-Version.
            Reutilize chave, conteúdo e versão original ao repetir uma tentativa. O recibo dura 24 horas por identidade e chave.
            Chaves diferentes representam tentativas diferentes, sujeitas às regras de CPF único e concorrência.

            ## Eventos e auditoria

            Abertura, edição efetiva e exclusão gravam auditoria e evento na mesma transação da conta.
            A entrega a Fraude e Cartões é assíncrona. Carteira e titular registram auditoria própria.
            O histórico de mensagens das demos está disponível no painel local da porta 5093.

            Consulte os usuários locais, exemplos curl e o fluxo de login no [guia de autenticação](https://github.com/RodolphoTrinca/contas/blob/main/docs/autenticacao.md).
            """;
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Access token Keycloak com audiência contas-api e cliente/role compatíveis com a operação."
        };
        document.Security = [new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        }];
        return Task.CompletedTask;
    }

    internal static Task DescribeOperation(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken ct)
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter is not OpenApiParameter value) continue;
            value.Description = value.Name switch
            {
                "id" => "UUID da conta. Conta ausente, excluída ou fora do escopo resulta em 404.",
                "accountId" => "UUID opcional para filtrar o histórico de uma conta, inclusive excluída.",
                "limit" => "Quantidade máxima de itens, de 1 a 100. Padrão 25.",
                "offset" => "Quantidade de contas a pular, a partir de zero. Padrão 0.",
                "before" => "Cursor positivo recebido em data.nextBefore para buscar registros mais antigos.",
                IdempotencyHeaders.Key => "UUID não vazio. Reutilize a mesma chave e o mesmo conteúdo no retry, dentro de 24 horas.",
                "Expected-Version" => "Versão positiva recebida na consulta da conta. No retry, preserve a versão da tentativa original.",
                _ => value.Description
            };
        }

        foreach (var (status, value) in operation.Responses ?? [])
        {
            if (value is not OpenApiResponse response) continue;
            response.Description = status switch
            {
                "200" => "Operação concluída. Resultado em data, error=null e traceId da requisição.",
                "201" => "Conta aberta. Retorna a conta em data e sua URL no cabeçalho Location.",
                "400" => "Entrada inválida. Consulte error.code, como request.invalid, account.invalid_cpf ou account.invalid_name.",
                "401" => "Autenticação ausente ou inválida. Código auth.required.",
                "403" => "Cliente ou role sem permissão. Código auth.forbidden.",
                "404" => "Conta ausente, excluída ou fora do escopo. Código account.not_found.",
                "409" => "Conflito de CPF, versão, vínculo ou chave idempotente. O código específico é descrito na operação.",
                "500" => "Falha inesperada. Código service.internal_error. Use traceId para correlação.",
                "503" => "Dependência temporariamente indisponível. Código service.unavailable.",
                _ => response.Description
            };
            if (status is not ("200" or "201")) continue;
            response.Headers ??= new Dictionary<string, IOpenApiHeader>();
            if (operation.Parameters?.Any(parameter => parameter.Name == IdempotencyHeaders.Key) == true)
                response.Headers[IdempotencyHeaders.Replayed] = Header("Presente com valor true quando a resposta vem de um recibo idempotente.");
            if (status == "201")
                response.Headers["Location"] = Header("Caminho /api/v1/accounts/{id} da conta criada.");
            if (operation.OperationId == "GetPermissions")
                response.Headers[IdempotencyHeaders.Scope] = Header("SHA-256 da identidade autenticada para separar tentativas no frontend.");
        }
        return Task.CompletedTask;
    }

    private static OpenApiHeader Header(string description) => new()
    {
        Description = description,
        Schema = new OpenApiSchema { Type = JsonSchemaType.String }
    };
}
