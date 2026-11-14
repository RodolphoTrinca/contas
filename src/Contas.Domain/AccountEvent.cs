namespace Contas.Domain;

public sealed record AccountEvent(Guid EventId, Guid AccountId, string Type, long Version,
    AccountStatus Status, DateTimeOffset OccurredAt);
