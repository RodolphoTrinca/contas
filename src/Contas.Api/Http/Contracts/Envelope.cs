namespace Contas.Api.Http;

internal sealed record Envelope<T>(T? Data, ApiError? Error, string TraceId);
