namespace Contas.Application;

public sealed record AccountChangeView(string Id, Guid AccountId, string Action, string Actor,
    DateTimeOffset OccurredAt);
