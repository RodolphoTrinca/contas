using Contas.Domain;

namespace Contas.Infrastructure.Persistence;

internal sealed class StoredAccount
{
    public string? ManagerSubject { get; set; }
    public string? OwnerSubject { get; set; }
    public Guid Id { get; init; }
    public string HolderName { get; set; } = "";
    public string Cpf { get; init; } = "";
    public AccountStatus Status { get; set; }
    public long Version { get; set; }
    public bool Deleted { get; set; }

    internal Account? ToDomain()
    {
        var cpf = Contas.Domain.Cpf.Create(Cpf);
        var name = AccountHolderName.Create(HolderName);
        var version = AccountVersion.Create(Version);
        if (!cpf.IsSuccess || !name.IsSuccess || !version.IsSuccess) return null;

        return Account.Restore(new AccountId(Id), name.Value, cpf.Value, Status, version.Value, Deleted,
            ReadSubject(ManagerSubject), ReadSubject(OwnerSubject));
    }

    internal void Apply(Account account)
    {
        HolderName = account.HolderName.Value;
        Status = account.Status;
        Version = account.Version.Value;
        Deleted = account.Deleted;
        ManagerSubject = account.Manager?.Value.ToString();
        OwnerSubject = account.Owner?.Value.ToString();
    }

    private static AccountSubject? ReadSubject(string? value)
        => Guid.TryParse(value, out var subject) ? AccountSubject.Create(subject).Value : null;
}
