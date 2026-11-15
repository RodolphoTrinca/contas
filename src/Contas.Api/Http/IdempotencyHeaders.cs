namespace Contas.Api.Http;

internal static class IdempotencyHeaders
{
    internal const string Key = "Idempotency-Key";
    internal const string Replayed = "Idempotency-Replayed";
}
