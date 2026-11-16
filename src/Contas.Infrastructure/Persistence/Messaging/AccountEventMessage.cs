using Contas.Domain;

namespace Contas.Infrastructure.Persistence;

internal sealed record AccountEventMessage(Guid EventId, Guid AccountId, string Type, long Version,
    AccountStatus Status, DateTimeOffset OccurredAt)
{
    internal static AccountEventMessage From(AccountEvent change)
        => new(change.EventId, change.AccountId.Value, change.Type.ToString(), change.Version.Value,
            change.Status, change.OccurredAt);
}
