using Contas.Application;
using Contas.Domain;
using Microsoft.EntityFrameworkCore;

namespace Contas.Infrastructure.Persistence;

internal sealed class AccountSession(AccountsDb db, AccountAccess? access) : IAccounts
{
    private StoredAccount? current;
    public async Task<Account?> Find(AccountId id, CancellationToken ct)
    {
        var accountId = id.Value;
        current = await AccountStore.Visible(db.Accounts, access).SingleOrDefaultAsync(row => row.Id == accountId && !row.Deleted, ct);
        return current?.ToDomain();
    }

    public Task<bool> CpfExists(Cpf cpf, CancellationToken ct)
        => db.Accounts.AnyAsync(row => row.Cpf == cpf.Value && !row.Deleted, ct);

    public void Add(Account account)
    {
        current = new StoredAccount { Id = account.Id.Value, Cpf = account.Cpf.Value };
        db.Accounts.Add(current);
    }

    public void Persist(Account account) => current?.Apply(account);
}
