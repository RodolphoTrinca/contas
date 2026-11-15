using Contas.Api.Security;
using Contas.Application;
using Contas.Application.Accounts.Commands;
using Contas.Application.Accounts.Queries;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using static Contas.Api.Http.RouteResponses;

namespace Contas.Api.Http.Onboarding;

internal static class AccountRoutes
{
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
            .RequireAuthorization(AuthorizationPolicies.AccountRead));

    private static void MapGetAccount(RouteGroupBuilder accounts)
        => Describe<AccountView>(accounts.MapGet("/{id:guid}", GetAccount)
            .RequireAuthorization(AuthorizationPolicies.AccountRead));

    private static void MapOpenAccount(RouteGroupBuilder accounts)
        => Describe<AccountView>(accounts.MapPost("/", OpenAccount)
            .RequireAuthorization(AuthorizationPolicies.AccountWrite), StatusCodes.Status201Created);

    private static void MapReviseAccount(RouteGroupBuilder accounts)
        => Describe<AccountView>(accounts.MapPut("/{id:guid}", ReviseAccount)
            .RequireAuthorization(AuthorizationPolicies.AccountWrite));

    private static void MapDeleteAccount(RouteGroupBuilder accounts)
        => Describe<DeletedAccount>(accounts.MapDelete("/{id:guid}", DeleteAccount)
            .RequireAuthorization(AuthorizationPolicies.AccountWrite));

    private static void MapTransferPortfolio(RouteGroupBuilder accounts)
        => Describe<bool>(accounts.MapPut("/{id:guid}/portfolio", TransferPortfolio)
            .RequireAuthorization(AuthorizationPolicies.Operator)
            .WithSummary("Atribui ou transfere a carteira"));

    private static void MapLinkOwner(RouteGroupBuilder accounts)
        => Describe<bool>(accounts.MapPut("/{id:guid}/owner", LinkOwner)
            .RequireAuthorization(AuthorizationPolicies.Operator)
            .WithSummary("Vincula a identidade do titular"));

    private static void MapListAccountChanges(RouteGroupBuilder accounts)
        => Describe<AccountChangePage>(accounts.MapGet("/history", ListAccountChanges)
            .RequireAuthorization(AuthorizationPolicies.Operator)
            .WithSummary("Consulta o histórico de alterações de contas"));

    private static void MapGetPermissions(RouteGroupBuilder accounts)
        => Describe<AccountPermissions>(accounts.MapGet("/permissions", GetPermissions)
            .RequireAuthorization(AuthorizationPolicies.AccountRead));

    private static async Task<IResult> ListAccounts(int? limit, int? offset, ISender sender, HttpContext http,
        CancellationToken ct)
        => Responses.From(http,
            await sender.Send(new ListAccountsQuery(limit, offset, AccountIdentity.Access(http.User)), ct),
            accounts => accounts);

    private static async Task<IResult> GetAccount(Guid id, ISender sender, HttpContext http, CancellationToken ct)
        => Responses.From(http, await sender.Send(new GetAccountQuery(id, AccountIdentity.Access(http.User)), ct),
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
        ReviseRequest body, ISender sender, HttpContext http, CancellationToken ct)
    {
        if (key == Guid.Empty) return InvalidKey(http);

        var command = new ReviseAccountCommand(AccountIdentity.Scope(http.User), key, id, body.HolderName, body.Status,
            AccountIdentity.Access(http.User));
        return Responses.Write(http, await sender.Send(command, ct), account => account);
    }

    private static async Task<IResult> DeleteAccount(Guid id, [FromHeader(Name = IdempotencyHeaders.Key)] Guid key,
        ISender sender, HttpContext http, CancellationToken ct)
    {
        if (key == Guid.Empty) return InvalidKey(http);

        var command = new DeleteAccountCommand(AccountIdentity.Scope(http.User), key, id,
            AccountIdentity.Access(http.User));
        return Responses.Write(http, await sender.Send(command, ct), account => new DeletedAccount(account.Id, true));
    }

    private static async Task<IResult> TransferPortfolio(Guid id, AssignmentRequest body, ISender sender,
        HttpContext http, CancellationToken ct)
    {
        var command = new TransferPortfolioCommand(id, body.Subject, AccountIdentity.Scope(http.User));
        return Responses.From(http, await sender.Send(command, ct), assigned => assigned);
    }

    private static async Task<IResult> LinkOwner(Guid id, AssignmentRequest body, ISender sender,
        HttpContext http, CancellationToken ct)
    {
        var command = new LinkAccountOwnerCommand(id, body.Subject, AccountIdentity.Scope(http.User));
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
        return Responses.Ok(http, new AccountPermissions(access is null or { IsManager: true }, access is null));
    }

    private static IResult InvalidKey(HttpContext http)
        => Responses.Fail(http, StatusCodes.Status400BadRequest, "idempotency.invalid_key",
            "Informe uma chave de idempotência UUID.");
}
