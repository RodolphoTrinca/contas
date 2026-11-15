namespace Contas.Api.Http;

internal static class RouteResponses
{
    private static readonly int[] FailureStatuses =
    [
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict,
        StatusCodes.Status500InternalServerError,
        StatusCodes.Status503ServiceUnavailable
    ];

    internal static void Describe<T>(RouteHandlerBuilder route, int status = StatusCodes.Status200OK)
    {
        route.Produces<Envelope<T>>(status);
        foreach (var failure in FailureStatuses)
            route.Produces<Envelope<object>>(failure);
    }
}
