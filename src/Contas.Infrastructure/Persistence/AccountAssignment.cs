using Contas.Application;
using Contas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Contas.Infrastructure.Persistence;

public sealed class AccountAssignment(AccountsDb db, ILogger<AccountAssignment> logger) : IAccountAssignment
{
    public Task<Result<bool>> Transfer(Guid accountId, Guid managerSubject, CancellationToken ct)
        => Assign(accountId, managerSubject, true, ct);

    public Task<Result<bool>> LinkOwner(Guid accountId, Guid ownerSubject, CancellationToken ct)
        => Assign(accountId, ownerSubject, false, ct);

    private async Task<Result<bool>> Assign(Guid accountId, Guid subject, bool manager, CancellationToken ct)
    {
        if (subject == Guid.Empty)
            return Result<bool>.Failure(new("account.invalid_subject", "Informe o ID do usuário no Keycloak.", ErrorKind.Validation));
        try
        {
            var account = await db.Accounts.SingleOrDefaultAsync(row => row.Id == accountId && !row.Deleted, ct);
            if (account is null) return Result<bool>.Failure(AccountErrors.Missing);
            if (manager) account.ManagerSubject = subject.ToString();
            else account.OwnerSubject = subject.ToString();
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<bool>.Failure(new("account.concurrent_change", "A conta mudou. Tente novamente.", ErrorKind.Conflict));
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Result<bool>.Failure(new("account.owner_already_linked", "O titular já possui uma conta vinculada.", ErrorKind.Conflict));
        }
        catch (Exception error) when (error is NpgsqlException or DbUpdateException)
        {
            logger.LogError(error, "Falha ao atualizar vínculo de conta");
            return Result<bool>.Failure(AccountErrors.Unavailable);
        }
    }
}
