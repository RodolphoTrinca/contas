using Contas.Api.Security;
using Contas.Application.Accounts.Queries;
using Contas.Domain;
using Mediator;
using static Contas.Api.Http.RouteResponses;

namespace Contas.Api.Http.Fraud;

internal static class FraudRoutes
{
    internal static void MapFraudRoutes(this RouteGroupBuilder v1)
    {
        Describe<FraudStatus>(v1.MapGet("/fraud/accounts/{id:guid}/status", GetAccountStatus)
            .WithName(nameof(GetAccountStatus))
            .WithSummary("Consultar status para Fraude")
            .WithDescription("Exige cliente fraud-api e role fraud-reader, com token client credentials. Retorna id e status active ou inactive. Conta ausente ou excluída retorna 404. Consultas podem usar Redis. Tokens de Cartões ou do cliente web recebem 403 nesta rota.")
            .RequireAuthorization(AuthorizationPolicies.Fraud).WithTags("Fraude"));
    }

    private static async Task<IResult> GetAccountStatus(Guid id, ISender sender, HttpContext http, CancellationToken ct)
        => Responses.From(http, await sender.Send(new GetAccountQuery(new AccountId(id)), ct),
            account => new FraudStatus(account.Id, account.Status));
}
