using Contas.Api.Security;
using Contas.Application.Cards.Queries;
using Contas.Domain;
using Mediator;
using static Contas.Api.Http.RouteResponses;

namespace Contas.Api.Http.Cards;

internal static class CardRoutes
{
    internal static void MapCardRoutes(this RouteGroupBuilder v1)
    {
        Describe<CardEligibility>(v1.MapGet("/cards/accounts/{id:guid}/eligibility", GetCardEligibility)
            .WithName(nameof(GetCardEligibility))
            .WithSummary("Consultar elegibilidade para Cartões")
            .WithDescription("Exige cliente cards-api e role cards-reader, com token client credentials. Retorna id e eligible=true quando a conta está ativa. A regra é demonstrativa. Conta ausente ou excluída retorna 404. Consultas podem usar Redis. Tokens de Fraude ou do cliente web recebem 403 nesta rota.")
            .RequireAuthorization(AuthorizationPolicies.Cards).WithTags("Cartões"));
    }

    private static async Task<IResult> GetCardEligibility(Guid id, ISender sender, HttpContext http, CancellationToken ct)
        => Responses.From(http, await sender.Send(new GetCardEligibilityQuery(new AccountId(id)), ct),
            eligibility => eligibility);
}
