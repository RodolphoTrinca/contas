using Contas.Domain;

namespace Contas.Application;

public interface IAccountAssignment
{
    Task<Result<bool>> Transfer(Guid accountId, Guid managerSubject, CancellationToken ct);
    Task<Result<bool>> LinkOwner(Guid accountId, Guid ownerSubject, CancellationToken ct);
}
