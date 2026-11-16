using System.Security.Cryptography;
using System.Text;
using Contas.Api.Security;
using Contas.Application;
using Contas.Application.Accounts.Commands;
using Contas.Application.Accounts.Queries;
using Contas.Domain;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using static Contas.Api.Http.RouteResponses;

namespace Contas.Api.Http.Onboarding;

internal static class AccountRoutes
{
    private const string ExpectedVersionHeader = "Expected-Version";

    internal const string Prefix = "/accounts";

    internal static void MapAccountRoutes(this RouteGroupBuilder v1)
    {
        var accounts = v1.MapGroup(Prefix).WithTags("Contas");

        MapAccountQueryRoutes(accounts);
        MapAccountCommandRoutes(accounts);
        MapAccountAdministrationRoutes(accounts);
    }

    private static void MapAccountQueryRoutes(RouteGroupBuilder accounts)
    {
        MapListAccounts(accounts);
        MapGetAccount(accounts);
    }

    private static void MapAccountCommandRoutes(RouteGroupBuilder accounts)
    {
        MapOpenAccount(accounts);
        MapReviseAccount(accounts);
        MapDeleteAccount(accounts);
    }

    private static void MapAccountAdministrationRoutes(RouteGroupBuilder accounts)
    {
        MapTransferPortfolio(accounts);
        MapLinkOwner(accounts);
        MapListAccountChanges(accounts);
        MapGetPermissions(accounts);
    }

    private static void MapListAccounts(RouteGroupBuilder accounts)
        => Describe<IReadOnlyList<AccountView>>(accounts.MapGet("/", ListAccounts)
            .WithName(nameof(ListAccounts))
            .WithSummary("Listar contas visíveis")
            .WithDescription("""
                Operador lista todas as contas, Gerente lista a própria carteira e Cliente lista a conta vinculada à sua identidade.
                A lista omite contas excluídas e mascara o CPF. A ordenação é por nome e UUID.
                Use limit entre 1 e 100 e offset a partir de zero. Os padrões são 25 e 0.
                A resposta contém um array em data. Busca e filtros da interface atuam sobre a página carregada.
                """)
            .RequireAuthorization(AuthorizationPolicies.AccountRead));

    private static void MapGetAccount(RouteGroupBuilder accounts)
        => Describe<AccountView>(accounts.MapGet("/{id:guid}", GetAccount)
            .WithName(nameof(GetAccount))
            .WithSummary("Consultar uma conta")
            .WithDescription("""
                Retorna UUID, nome, CPF mascarado, status e versão da conta.
                Operador consulta qualquer conta, Gerente consulta a própria carteira e Cliente consulta a conta vinculada.
                Conta ausente, excluída ou fora do escopo retorna 404 com account.not_found.
                Consultas do Operador podem usar Redis. Consultas de Gerente e Cliente verificam os vínculos no PostgreSQL.
                """)
            .RequireAuthorization(AuthorizationPolicies.AccountRead));

    private static void MapOpenAccount(RouteGroupBuilder accounts)
        => Describe<AccountView>(accounts.MapPost("/", OpenAccount)
            .WithName(nameof(OpenAccount))
            .WithSummary("Abrir uma conta")
            .WithDescription("""
                Exige o cliente contas-web com role accounts-operator ou accounts-manager.
                O nome deve ter de 2 a 120 caracteres. O CPF aceita 11 dígitos ou o formato pontuado e deve ter dígitos verificadores válidos.
                A conta começa active, com versão 1. Abertura pelo Gerente atribui automaticamente a conta à sua carteira.

                Envie Idempotency-Key com um UUID novo para esta operação. Em retry, preserve chave e conteúdo.
                O recibo dura 24 horas por identidade e chave. Reuso com conteúdo diferente retorna 409 com idempotency.key_conflict.
                CPF já usado por uma conta não excluída retorna 409 com account.duplicate_cpf.
                O sucesso retorna 201, Location e a conta em data. Auditoria e evento são gravados na mesma transação.

                Exemplo de corpo com dados fictícios
                ```json
                {"holderName":"Ana Demonstração","cpf":"52998224725"}
                ```
                """)
            .RequireAuthorization(AuthorizationPolicies.AccountWrite), StatusCodes.Status201Created);

    private static void MapReviseAccount(RouteGroupBuilder accounts)
        => Describe<AccountView>(accounts.MapPut("/{id:guid}", ReviseAccount)
            .WithName(nameof(ReviseAccount))
            .WithSummary("Editar nome e status")
            .WithDescription("""
                Operador edita qualquer conta e Gerente edita contas da própria carteira.
                Envie holderName e status, com active ou inactive. O CPF é imutável.
                Envie Idempotency-Key e Expected-Version com a versão recebida na consulta.
                No retry, preserve chave, conteúdo e versão original. O recibo é consultado antes da versão atual.

                Versão obsoleta retorna 409 com account.stale_version. Conflito durante a gravação retorna account.concurrent_change.
                Uma alteração efetiva incrementa a versão e registra auditoria e evento. Repetir o estado atual mantém a versão.

                Exemplo de corpo
                ```json
                {"holderName":"Ana Demonstração","status":"inactive"}
                ```
                """)
            .RequireAuthorization(AuthorizationPolicies.AccountWrite));

    private static void MapDeleteAccount(RouteGroupBuilder accounts)
        => Describe<DeletedAccount>(accounts.MapDelete("/{id:guid}", DeleteAccount)
            .WithName(nameof(DeleteAccount))
            .WithSummary("Excluir uma conta")
            .WithDescription("""
                Operador exclui qualquer conta e Gerente exclui contas da própria carteira.
                Envie Idempotency-Key e Expected-Version. Em retry, reutilize a chave e a versão originais.
                A exclusão é lógica e terminal para o UUID. O CPF fica disponível para uma nova abertura.
                O sucesso retorna 200 com id e deleted=true em data. A auditoria continua consultável pelo Operador.
                A exclusão grava um evento AccountDeleted para entrega assíncrona.
                """)
            .RequireAuthorization(AuthorizationPolicies.AccountWrite));

    private static void MapTransferPortfolio(RouteGroupBuilder accounts)
        => Describe<bool>(accounts.MapPut("/{id:guid}/portfolio", TransferPortfolio)
            .WithName(nameof(TransferPortfolio))
            .WithSummary("Atribuir ou transferir carteira")
            .WithDescription("""
                Exige contas-web com role accounts-operator.
                Envie subject com o UUID do gerente no Keycloak. Cabe ao Operador conferir a identidade e sua role.
                Após a transferência, consultas e reenvios do gerente anterior passam a respeitar a carteira atual.
                Repetir o mesmo vínculo é uma operação sem efeito. Essa rota usa concorrência do EF Core e dispensa os cabeçalhos Idempotency-Key e Expected-Version.
                O sucesso retorna true em data e a mudança efetiva gera auditoria PortfolioAssigned.

                Exemplo para o Gerente A do realm local
                ```json
                {"subject":"74113d8a-7870-4e36-a8f8-141a69a8a601"}
                ```
                """)
            .RequireAuthorization(AuthorizationPolicies.Operator));

    private static void MapLinkOwner(RouteGroupBuilder accounts)
        => Describe<bool>(accounts.MapPut("/{id:guid}/owner", LinkOwner)
            .WithName(nameof(LinkOwner))
            .WithSummary("Vincular a identidade do titular")
            .WithDescription("""
                Exige contas-web com role accounts-operator.
                Envie subject com o UUID do cliente no Keycloak. Cabe ao Operador conferir a identidade e sua role.
                Uma identidade pode estar vinculada a uma conta não excluída. Conflito retorna 409 com account.owner_already_linked.
                Repetir o mesmo vínculo é uma operação sem efeito. Essa rota usa concorrência do EF Core e dispensa os cabeçalhos Idempotency-Key e Expected-Version.
                O sucesso retorna true em data e a mudança efetiva gera auditoria OwnerLinked.

                Exemplo para o Cliente do realm local
                ```json
                {"subject":"74113d8a-7870-4e36-a8f8-141a69a8a603"}
                ```
                """)
            .RequireAuthorization(AuthorizationPolicies.Operator));

    private static void MapListAccountChanges(RouteGroupBuilder accounts)
        => Describe<AccountChangePage>(accounts.MapGet("/history", ListAccountChanges)
            .WithName(nameof(ListAccountChanges))
            .WithSummary("Consultar o histórico de alterações")
            .WithDescription("""
                Exige contas-web com role accounts-operator. Inclui alterações de contas excluídas.
                Cada item contém ID do registro, ID da conta, ação, autor azp:sub e horário UTC. O seed usa system:seed.
                As ações incluem AccountCreated, AccountUpdated, AccountDeleted, PortfolioAssigned e OwnerLinked.
                Consultas, falhas, replays e alterações sem efeito ficam fora da trilha.

                Use accountId para filtrar uma conta. limit aceita de 1 a 100, com padrão 25.
                Os registros chegam do mais recente para o mais antigo. Envie data.nextBefore como before para avançar.
                nextBefore=null indica o fim. A resposta usa Cache-Control: no-store.
                """)
            .RequireAuthorization(AuthorizationPolicies.Operator));

    private static void MapGetPermissions(RouteGroupBuilder accounts)
        => Describe<AccountPermissions>(accounts.MapGet("/permissions", GetPermissions)
            .WithName(nameof(GetPermissions))
            .WithSummary("Consultar permissões da sessão")
            .WithDescription("""
                Exige contas-web e um perfil de leitura de contas.
                Retorna canWrite para Operador e Gerente e canAssign somente para Operador.
                O cabeçalho Idempotency-Scope contém a impressão SHA-256 da identidade, usada para separar tentativas no frontend.
                As permissões orientam a interface. A API verifica autorização e acesso à conta em cada operação.
                """)
            .RequireAuthorization(AuthorizationPolicies.AccountRead));

    private static async Task<IResult> ListAccounts(int? limit, int? offset, ISender sender, HttpContext http,
        CancellationToken ct)
        => Responses.From(http,
            await sender.Send(new ListAccountsQuery(limit, offset, AccountIdentity.Access(http.User)), ct),
            accounts => accounts);

    private static async Task<IResult> GetAccount(Guid id, ISender sender, HttpContext http, CancellationToken ct)
        => Responses.From(http, await sender.Send(new GetAccountQuery(new AccountId(id), AccountIdentity.Access(http.User)), ct),
            account => account);

    private static async Task<IResult> OpenAccount([FromHeader(Name = IdempotencyHeaders.Key)] Guid key,
        OpenRequest body, ISender sender, HttpContext http, CancellationToken ct)
    {
        if (key == Guid.Empty) return InvalidKey(http);

        var command = new OpenAccountCommand(AccountIdentity.Scope(http.User), key, body.HolderName, body.Cpf,
            AccountIdentity.Access(http.User));
        return Responses.Write(http, await sender.Send(command, ct), account => account, StatusCodes.Status201Created);
    }

    private static async Task<IResult> ReviseAccount(Guid id, [FromHeader(Name = IdempotencyHeaders.Key)] Guid key,
        [FromHeader(Name = ExpectedVersionHeader)] long expectedVersion,
        ReviseRequest body, ISender sender, HttpContext http, CancellationToken ct)
    {
        if (key == Guid.Empty) return InvalidKey(http);
        var version = AccountVersion.Create(expectedVersion);
        if (!version.IsSuccess) return InvalidVersion(http, version.Error);

        var command = new ReviseAccountCommand(AccountIdentity.Scope(http.User), key, new AccountId(id), version.Value, body.HolderName, body.Status,
            AccountIdentity.Access(http.User));
        return Responses.Write(http, await sender.Send(command, ct), account => account);
    }

    private static async Task<IResult> DeleteAccount(Guid id, [FromHeader(Name = IdempotencyHeaders.Key)] Guid key,
        [FromHeader(Name = ExpectedVersionHeader)] long expectedVersion,
        ISender sender, HttpContext http, CancellationToken ct)
    {
        if (key == Guid.Empty) return InvalidKey(http);
        var version = AccountVersion.Create(expectedVersion);
        if (!version.IsSuccess) return InvalidVersion(http, version.Error);

        var command = new DeleteAccountCommand(AccountIdentity.Scope(http.User), key, new AccountId(id), version.Value,
            AccountIdentity.Access(http.User));
        return Responses.Write(http, await sender.Send(command, ct), account => new DeletedAccount(account.Id, true));
    }

    private static async Task<IResult> TransferPortfolio(Guid id, AssignmentRequest body, ISender sender,
        HttpContext http, CancellationToken ct)
    {
        var command = new TransferPortfolioCommand(new AccountId(id), body.Subject, AccountIdentity.Scope(http.User));
        return Responses.From(http, await sender.Send(command, ct), assigned => assigned);
    }

    private static async Task<IResult> LinkOwner(Guid id, AssignmentRequest body, ISender sender,
        HttpContext http, CancellationToken ct)
    {
        var command = new LinkAccountOwnerCommand(new AccountId(id), body.Subject, AccountIdentity.Scope(http.User));
        return Responses.From(http, await sender.Send(command, ct), linked => linked);
    }

    private static async Task<IResult> ListAccountChanges(Guid? accountId, long? before, int? limit, ISender sender,
        HttpContext http, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var query = new ListAccountChangesQuery(accountId, before, limit);
        return Responses.From(http, await sender.Send(query, ct), page => page);
    }

    private static IResult GetPermissions(HttpContext http)
    {
        var access = AccountIdentity.Access(http.User);
        var scope = AccountIdentity.Scope(http.User);
        http.Response.Headers[IdempotencyHeaders.Scope] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));
        return Responses.Ok(http, new AccountPermissions(access is null or { IsManager: true }, access is null));
    }

    private static IResult InvalidVersion(HttpContext http, Error error)
        => Responses.Fail(http, StatusCodes.Status400BadRequest, error.Code,
            "Informe a versão da conta no cabeçalho Expected-Version.");

    private static IResult InvalidKey(HttpContext http)
        => Responses.Fail(http, StatusCodes.Status400BadRequest, "idempotency.invalid_key",
            "Informe uma chave de idempotência UUID.");
}
