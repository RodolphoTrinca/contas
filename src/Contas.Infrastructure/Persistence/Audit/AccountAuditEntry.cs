namespace Contas.Infrastructure.Persistence;

internal sealed class AccountAuditEntry(Guid accountId, string actor, string action, DateTimeOffset occurredAt)
{
    public long Id { get; private set; }
    public Guid AccountId { get; private set; } = accountId;
    public string Actor { get; private set; } = actor;
    public string Action { get; private set; } = action;
    public DateTimeOffset OccurredAt { get; private set; } = occurredAt;
}
