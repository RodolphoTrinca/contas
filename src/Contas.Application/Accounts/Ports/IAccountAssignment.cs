using Contas.Domain;

namespace Contas.Application;

public interface IAccountAssignment
{
    Task<Result<bool>> Transfer(AccountId accountId, AccountSubject managerSubject, string actor, CancellationToken ct);
    Task<Result<bool>> LinkOwner(AccountId accountId, AccountSubject ownerSubject, string actor, CancellationToken ct);
}
