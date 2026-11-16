namespace Contas.Application;

public sealed record IdempotentWrite(string Scope, Guid Key, string Fingerprint)
{
    public bool Matches(string fingerprint) => StringComparer.Ordinal.Equals(Fingerprint, fingerprint);
}
