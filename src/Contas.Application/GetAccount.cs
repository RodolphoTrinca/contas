using Contas.Domain;

namespace Contas.Application;

public sealed class GetAccount(IAccountStore store)
{
    public Task<Result<AccountView>> Execute(Guid id, CancellationToken cancellationToken)
        => store.Get(id, cancellationToken);
}
