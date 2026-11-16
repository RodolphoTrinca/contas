using Contas.Application;
using Contas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Contas.Infrastructure.Persistence;

public sealed class AccountAssignment(AccountsDb db, TimeProvider clock, ILogger<AccountAssignment> logger) : IAccountAssignment
{
    public Task<Result<bool>> Transfer(AccountId id, AccountSubject managerSubject, string actor, CancellationToken ct)
        => Assign(id, managerSubject, static (account, subject) => account.TransferPortfolio(subject), "PortfolioAssigned", actor, ct);

    public Task<Result<bool>> LinkOwner(AccountId id, AccountSubject ownerSubject, string actor, CancellationToken ct)
        => Assign(id, ownerSubject, static (account, subject) => account.LinkOwner(subject), "OwnerLinked", actor, ct);

    private async Task<Result<bool>> Assign(AccountId id, AccountSubject subject, Func<Account, AccountSubject, Result<bool>> apply,
        string action, string actor, CancellationToken ct)
    {
        var accountId = id.Value;
        try
        {
            var row = await db.Accounts.SingleOrDefaultAsync(item => item.Id == accountId && !item.Deleted, ct);
            if (row?.ToDomain() is not { } account) return Result<bool>.Failure(AccountErrors.Missing);

            var changed = apply(account, subject);
            if (!changed.IsSuccess) return changed;
            if (!changed.Value) return Result<bool>.Success(true);

            row.Apply(account);
            db.AuditEntries.Add(new(accountId, actor, action, clock.GetUtcNow()));
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
