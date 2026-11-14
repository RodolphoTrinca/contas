using Contas.Domain;

namespace Contas.Application;

public sealed class GetAccount(IAccountStore store)
{
    public Task<Result<AccountView>> Execute(Guid id, CancellationToken cancellationToken, AccountAccess? access = null)
        => store.Get(id, cancellationToken, access);
}
