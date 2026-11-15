using Contas.Api.Security;
using Contas.Application.Accounts.Queries;
using Contas.Domain;
using Mediator;
using static Contas.Api.Http.RouteResponses;

namespace Contas.Api.Http.Cards;

internal static class CardRoutes
{
    internal static void MapCardRoutes(this RouteGroupBuilder v1)
    {
        Describe<CardEligibility>(v1.MapGet("/cards/accounts/{id:guid}/eligibility", GetCardEligibility)
            .RequireAuthorization(AuthorizationPolicies.Cards).WithTags("Cartões"));
    }

    private static async Task<IResult> GetCardEligibility(Guid id, ISender sender, HttpContext http, CancellationToken ct)
        => Responses.From(http, await sender.Send(new GetAccountQuery(id), ct),
            account => new CardEligibility(account.Id, account.Status == AccountStatus.Active));
}
