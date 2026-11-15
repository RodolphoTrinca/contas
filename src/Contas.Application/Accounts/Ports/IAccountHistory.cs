using Contas.Domain;

namespace Contas.Application;

public interface IAccountHistory
{
    Task<Result<AccountChangePage>> List(Guid? accountId, long? before, int limit, CancellationToken ct);
}
