using Contas.Domain;

namespace Contas.Application;

public sealed record AccountAccess(AccountSubject? Subject, AccountAccessKind Kind)
{
    public bool IsManager => Kind == AccountAccessKind.ManagedPortfolio;
}
