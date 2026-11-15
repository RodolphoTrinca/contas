namespace Contas.Application;

public sealed record AccountChangePage(IReadOnlyList<AccountChangeView> Items, string? NextBefore);
