using Contas.Domain;
using Contas.Application;

namespace Contas.Api.Http;

internal static class Responses
{
    internal static IResult Ok<T>(HttpContext http, T data, int status = 200)
        => Results.Json(new Envelope<T>(data, null, http.TraceIdentifier), statusCode: status);

    internal static IResult Fail(HttpContext http, int status, string code, string message)
        => Results.Json(new Envelope<object>(null, new(code, message), http.TraceIdentifier), statusCode: status);

    internal static IResult From<T, TOutput>(HttpContext http, Result<T> result, Func<T, TOutput> select)
        => result.IsSuccess ? Ok(http, select(result.Value)) : Fail(http, result.Error);

    internal static IResult Fail(HttpContext http, Error error) => Fail(http, error.Kind switch
    {
        ErrorKind.Validation => 400,
        ErrorKind.NotFound => 404,
        ErrorKind.Conflict => 409,
        _ => 503
    }, error.Code, error.Message);

    internal static IResult Write<T>(HttpContext http, Result<WriteReceipt> result,
        Func<AccountView, T> select, int status = 200)
    {
        if (!result.IsSuccess) return Fail(http, result.Error);
        if (result.Value.Replayed) http.Response.Headers["Idempotency-Replayed"] = "true";
        if (status == 201) http.Response.Headers.Location = $"/api/v1/accounts/{result.Value.Account.Id}";
        return Ok(http, select(result.Value.Account), status);
    }

    internal static Task Status(HttpContext http)
    {
        var status = http.Response.StatusCode;
        var (code, message) = status switch
        {
            400 => ("request.invalid", "A requisição contém dados inválidos."),
            401 => ("auth.required", "Autenticação válida é necessária."),
            403 => ("auth.forbidden", "Permissão insuficiente para esta operação."),
            404 => ("request.not_found", "Rota não encontrada."),
            405 => ("request.method_not_allowed", "Método não permitido."),
            415 => ("request.unsupported_media_type", "Formato de conteúdo não aceito."),
            _ => ("request.failed", "Não foi possível concluir a requisição.")
        };
        return Fail(http, status, code, message).ExecuteAsync(http);
    }
}
