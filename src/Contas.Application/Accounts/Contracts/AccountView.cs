using Contas.Domain;

namespace Contas.Application;

public sealed record AccountView(Guid Id, string HolderName, string Cpf, AccountStatus Status, long Version)
{
    public static AccountView From(Account account) =>
        new(account.Id.Value, account.HolderName.Value, "***.***.***-" + account.Cpf.Value[^2..], account.Status, account.Version.Value);
}
