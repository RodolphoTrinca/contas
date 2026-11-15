using Contas.Application;
using Contas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Contas.Infrastructure.Persistence;

public sealed class AccountAssignment(AccountsDb db, TimeProvider clock, ILogger<AccountAssignment> logger) : IAccountAssignment
{
    public Task<Result<bool>> Transfer(Guid accountId, Guid managerSubject, string actor, CancellationToken ct)
        => Assign(accountId, managerSubject, true, actor, ct);

    public Task<Result<bool>> LinkOwner(Guid accountId, Guid ownerSubject, string actor, CancellationToken ct)
        => Assign(accountId, ownerSubject, false, actor, ct);

    private async Task<Result<bool>> Assign(Guid accountId, Guid subject, bool manager, string actor, CancellationToken ct)
    {
        if (subject == Guid.Empty)
            return Result<bool>.Failure(new("account.invalid_subject", "Informe o ID do usuário no Keycloak.", ErrorKind.Validation));
        try
        {
            var account = await db.Accounts.SingleOrDefaultAsync(row => row.Id == accountId && !row.Deleted, ct);
            if (account is null) return Result<bool>.Failure(AccountErrors.Missing);
            var next = subject.ToString();
            var previous = manager ? account.ManagerSubject : account.OwnerSubject;
            if (previous == next) return Result<bool>.Success(true);

            if (manager) account.ManagerSubject = next;
            else account.OwnerSubject = next;
            db.AuditEntries.Add(new(accountId, actor,
                manager ? "PortfolioAssigned" : "OwnerLinked", clock.GetUtcNow()));
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
