using Contas.Domain;

namespace Contas.Application;

public interface IAccountAssignment
{
    Task<Result<bool>> Transfer(Guid accountId, Guid managerSubject, string actor, CancellationToken ct);
    Task<Result<bool>> LinkOwner(Guid accountId, Guid ownerSubject, string actor, CancellationToken ct);
}
