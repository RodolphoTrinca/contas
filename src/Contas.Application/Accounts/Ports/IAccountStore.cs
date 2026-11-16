using Contas.Domain;

namespace Contas.Application;

public interface IAccountStore
{
    Task<Result<WriteReceipt>> Write(IdempotentWrite operation,
        Func<IAccounts, Task<Result<Account>>> execute, CancellationToken cancellationToken, AccountAccess? access = null);

    Task<Result<AccountView>> Get(AccountId id, CancellationToken cancellationToken, AccountAccess? access = null);
    Task<Result<IReadOnlyList<AccountView>>> List(int limit, int offset, CancellationToken cancellationToken, AccountAccess? access = null);
}
