namespace Contas.Infrastructure.Persistence;

internal sealed record IdempotencyRecord(string Scope, Guid Key, string Fingerprint, string Response, DateTimeOffset ExpiresAt);
