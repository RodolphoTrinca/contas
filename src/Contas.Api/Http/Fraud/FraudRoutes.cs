using Contas.Api.Security;
using Contas.Application.Accounts.Queries;
using Mediator;
using static Contas.Api.Http.RouteResponses;

namespace Contas.Api.Http.Fraud;

internal static class FraudRoutes
{
    internal static void MapFraudRoutes(this RouteGroupBuilder v1)
    {
        Describe<FraudStatus>(v1.MapGet("/fraud/accounts/{id:guid}/status", GetAccountStatus)
            .RequireAuthorization(AuthorizationPolicies.Fraud).WithTags("Fraude"));
    }

    private static async Task<IResult> GetAccountStatus(Guid id, ISender sender, HttpContext http, CancellationToken ct)
        => Responses.From(http, await sender.Send(new GetAccountQuery(id), ct),
            account => new FraudStatus(account.Id, account.Status));
}
