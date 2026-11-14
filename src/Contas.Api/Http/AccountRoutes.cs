using Contas.Api.Security;
using Contas.Application;
using Contas.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Contas.Api.Http;

internal static class AccountRoutes
{
    internal static void MapV1(this WebApplication app)
    {
        var v1 = app.MapGroup("/api/v1");
        var accounts = v1.MapGroup("/accounts").RequireAuthorization("operator").WithTags("Contas");
        Describe<IReadOnlyList<AccountView>>(accounts.MapGet("/", async (int? limit, int? offset, ListAccounts useCase, HttpContext http, CancellationToken ct)
            => Responses.From(http, await useCase.Execute(limit, offset, ct), x => x)));
        Describe<AccountView>(accounts.MapGet("/{id:guid}", async (Guid id, GetAccount useCase, HttpContext http, CancellationToken ct)
            => Responses.From(http, await useCase.Execute(id, ct), x => x)));
        Describe<AccountView>(accounts.MapPost("/", async ([FromHeader(Name = "Idempotency-Key")] Guid key, OpenRequest body,
            OpenAccount useCase, HttpContext http, CancellationToken ct) =>
            key == Guid.Empty ? InvalidKey(http) : Responses.Write(http,
                await useCase.Execute(AccountIdentity.Scope(http.User), key, body.HolderName, body.Cpf, ct), x => x, 201)), 201);
        Describe<AccountView>(accounts.MapPut("/{id:guid}", async (Guid id, [FromHeader(Name = "Idempotency-Key")] Guid key,
            ReviseRequest body, ReviseAccount useCase, HttpContext http, CancellationToken ct) =>
            key == Guid.Empty ? InvalidKey(http) : Responses.Write(http,
                await useCase.Execute(AccountIdentity.Scope(http.User), key, id, body.HolderName, body.Status, ct), x => x)));
        Describe<DeletedAccount>(accounts.MapDelete("/{id:guid}", async (Guid id, [FromHeader(Name = "Idempotency-Key")] Guid key,
            DeleteAccount useCase, HttpContext http, CancellationToken ct) =>
            key == Guid.Empty ? InvalidKey(http) : Responses.Write(http,
                await useCase.Execute(AccountIdentity.Scope(http.User), key, id, ct), x => new DeletedAccount(x.Id, true))));

        Describe<FraudStatus>(v1.MapGet("/fraud/accounts/{id:guid}/status", async (Guid id, GetAccount useCase, HttpContext http, CancellationToken ct)
            => Responses.From(http, await useCase.Execute(id, ct), x => new FraudStatus(x.Id, x.Status)))
            .RequireAuthorization("fraud").WithTags("Fraude"));
        Describe<CardEligibility>(v1.MapGet("/cards/accounts/{id:guid}/eligibility", async (Guid id, GetAccount useCase, HttpContext http, CancellationToken ct)
            => Responses.From(http, await useCase.Execute(id, ct), x => new CardEligibility(x.Id, x.Status == AccountStatus.Active)))
            .RequireAuthorization("cards").WithTags("Cartões"));
    }

    private static IResult InvalidKey(HttpContext http)
        => Responses.Fail(http, 400, "idempotency.invalid_key", "Informe uma chave de idempotência UUID.");

    private static void Describe<T>(RouteHandlerBuilder route, int status = 200)
    {
        route.Produces<Envelope<T>>(status);
        foreach (var failure in new[] { 400, 401, 403, 404, 409, 500, 503 })
            route.Produces<Envelope<object>>(failure);
    }
}
