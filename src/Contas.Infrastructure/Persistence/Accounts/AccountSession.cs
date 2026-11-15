using Contas.Application;
using Contas.Domain;
using Microsoft.EntityFrameworkCore;

namespace Contas.Infrastructure.Persistence;

internal sealed class AccountSession(AccountsDb db, AccountAccess? access) : IAccounts
{
    private StoredAccount? current;
    public async Task<Account?> Find(Guid id, CancellationToken ct)
    {
        current = await AccountStore.Visible(db.Accounts, access).SingleOrDefaultAsync(row => row.Id == id && !row.Deleted, ct);
        if (current is null) return null;
        var cpf = Cpf.Create(current.Cpf);
        return cpf.IsSuccess
            ? Account.Restore(current.Id, current.HolderName, cpf.Value, current.Status, current.Version, current.Deleted)
            : null;
    }

    public Task<bool> CpfExists(string cpf, CancellationToken ct)
        => db.Accounts.AnyAsync(row => row.Cpf == cpf && !row.Deleted, ct);

    public void Add(Account account)
    {
        current = new StoredAccount { Id = account.Id, Cpf = account.Cpf.Value, ManagerSubject = access is { IsManager: true } ? access.Subject : null };
        db.Accounts.Add(current);
    }

    public void Persist(Account account)
    {
        if (current is null) return;
        current.HolderName = account.HolderName;
        current.Status = account.Status;
        current.Version = account.Version;
        current.Deleted = account.Deleted;
    }
}
