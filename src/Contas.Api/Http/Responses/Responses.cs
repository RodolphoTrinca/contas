using Contas.Domain;
using Contas.Application;

namespace Contas.Api.Http;

internal static class Responses
{
    internal static IResult Ok<T>(HttpContext http, T data, int status = StatusCodes.Status200OK)
        => Results.Json(new Envelope<T>(data, null, http.TraceIdentifier), statusCode: status);

    internal static IResult Fail(HttpContext http, int status, string code, string message)
        => Results.Json(new Envelope<object>(null, new(code, message), http.TraceIdentifier), statusCode: status);

    internal static IResult From<T, TOutput>(HttpContext http, Result<T> result, Func<T, TOutput> select)
        => result.IsSuccess ? Ok(http, select(result.Value)) : Fail(http, result.Error);

    internal static IResult Fail(HttpContext http, Error error) => Fail(http, error.Kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status503ServiceUnavailable
    }, error.Code, error.Message);

    internal static IResult Write<T>(HttpContext http, Result<WriteReceipt> result,
        Func<AccountView, T> select, int status = StatusCodes.Status200OK)
    {
        if (!result.IsSuccess) return Fail(http, result.Error);
        if (result.Value.Replayed) http.Response.Headers[IdempotencyHeaders.Replayed] = "true";
        if (status == StatusCodes.Status201Created)
            http.Response.Headers.Location = $"{ApiV1Routes.Prefix}{Onboarding.AccountRoutes.Prefix}/{result.Value.Account.Id}";
        return Ok(http, select(result.Value.Account), status);
    }

    internal static Task Status(HttpContext http)
    {
        var status = http.Response.StatusCode;
        var (code, message) = status switch
        {
            StatusCodes.Status400BadRequest => ("request.invalid", "A requisição contém dados inválidos."),
            StatusCodes.Status401Unauthorized => ("auth.required", "Autenticação válida é necessária."),
            StatusCodes.Status403Forbidden => ("auth.forbidden", "Permissão insuficiente para esta operação."),
            StatusCodes.Status404NotFound => ("request.not_found", "Rota não encontrada."),
            StatusCodes.Status405MethodNotAllowed => ("request.method_not_allowed", "Método não permitido."),
            StatusCodes.Status415UnsupportedMediaType => ("request.unsupported_media_type", "Formato de conteúdo não aceito."),
            _ => ("request.failed", "Não foi possível concluir a requisição.")
        };
        return Fail(http, status, code, message).ExecuteAsync(http);
    }
}
