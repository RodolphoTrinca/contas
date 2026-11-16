namespace Contas.Domain;

public sealed record AccountEvent(Guid EventId, AccountId AccountId, AccountEventKind Type, AccountVersion Version,
    AccountStatus Status, DateTimeOffset OccurredAt);
