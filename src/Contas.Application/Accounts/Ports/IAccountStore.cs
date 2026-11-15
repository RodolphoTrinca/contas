using Contas.Domain;

namespace Contas.Application;

public interface IAccountStore
{
    Task<Result<WriteReceipt>> Write(string scope, Guid key, string fingerprint,
        Func<IAccounts, Task<Result<Account>>> execute, CancellationToken cancellationToken, AccountAccess? access = null);

    Task<Result<AccountView>> Get(Guid id, CancellationToken cancellationToken, AccountAccess? access = null);
    Task<Result<IReadOnlyList<AccountView>>> List(int limit, int offset, CancellationToken cancellationToken, AccountAccess? access = null);
}
