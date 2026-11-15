using Contas.Api.Http.Cards;
using Contas.Api.Http.Fraud;
using Contas.Api.Http.Onboarding;

namespace Contas.Api.Http;

internal static class ApiV1Routes
{
    internal const string Prefix = "/api/v1";

    internal static void MapV1(this WebApplication app)
    {
        var v1 = app.MapGroup(Prefix);
        v1.MapAccountRoutes();
        v1.MapFraudRoutes();
        v1.MapCardRoutes();
    }
}
