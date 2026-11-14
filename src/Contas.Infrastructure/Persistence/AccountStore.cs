using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Contas.Application;
using Contas.Domain;
using Contas.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Contas.Infrastructure.Persistence;

public sealed class AccountStore(AccountsDb db, AccountCache cache, TimeProvider clock,
    ILogger<AccountStore> logger) : IAccountStore
{
    public async Task<Result<WriteReceipt>> Write(string scope, Guid key, string fingerprint,
        Func<IAccounts, Task<Result<Account>>> execute, CancellationToken ct)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var lockId = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(scope + ":" + key)));
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
            var now = clock.GetUtcNow();
            var existing = await db.Receipts.FindAsync([scope, key], ct);
            if (existing is not null && existing.ExpiresAt > now)
            {
                if (existing.Fingerprint != fingerprint)
                    return Result<WriteReceipt>.Failure(new("idempotency.key_conflict", "A chave já foi usada em outra requisição.", ErrorKind.Conflict));
                var previous = JsonSerializer.Deserialize<AccountView>(existing.Response);
                return previous is not null
                    ? Result<WriteReceipt>.Success(new(previous, true))
                    : Result<WriteReceipt>.Failure(AccountErrors.Unavailable);
            }

            var session = new AccountSession(db);
            var result = await execute(session);
            if (!result.IsSuccess) return Result<WriteReceipt>.Failure(result.Error);

            var account = result.Value;
            if (account.PendingEvent is { } change)
            {
                session.Persist(account);
                db.Outbox.Add(new OutboxRecord { EventId = change.EventId, Payload = JsonSerializer.Serialize(change) });
            }

            var view = AccountView.From(account);
            if (existing is not null)
                await db.Receipts.Where(row => row.Scope == scope && row.Key == key).ExecuteDeleteAsync(ct);
            if (existing is not null)
                db.Entry(existing).State = EntityState.Detached;
            db.Receipts.Add(new(scope, key, fingerprint, JsonSerializer.Serialize(view), now.AddDays(1)));
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            if (account.PendingEvent is not null) await cache.Invalidate(account.Id);
            return Result<WriteReceipt>.Success(new(view, false));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<WriteReceipt>.Failure(new("account.concurrent_change", "A conta mudou. Consulte a versão atual e tente novamente.", ErrorKind.Conflict));
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "accounts_cpf_current" })
        {
            return Result<WriteReceipt>.Failure(AccountErrors.Duplicate);
        }
        catch (Exception error) when (error is NpgsqlException or DbUpdateException)
        {
            logger.LogError(error, "Falha ao persistir a operação de conta");
            return Result<WriteReceipt>.Failure(AccountErrors.Unavailable);
        }
    }

    public async Task<Result<AccountView>> Get(Guid id, CancellationToken ct)
    {
        var lookup = await cache.Read(id);
        if (lookup.Account is not null) return Result<AccountView>.Success(lookup.Account);
        try
        {
            var account = await Project(db.Accounts.AsNoTracking().Where(row => !row.Deleted && row.Id == id)).SingleOrDefaultAsync(ct);
            if (account is null) return Result<AccountView>.Failure(AccountErrors.Missing);
            await cache.Fill(id, lookup.Generation, account);
            return Result<AccountView>.Success(account);
        }
        catch (NpgsqlException error)
        {
            logger.LogError(error, "Falha ao consultar conta");
            return Result<AccountView>.Failure(AccountErrors.Unavailable);
        }
    }

    public async Task<Result<IReadOnlyList<AccountView>>> List(int limit, int offset, CancellationToken ct)
    {
        try
        {
            var rows = db.Accounts.AsNoTracking().Where(row => !row.Deleted)
                .OrderBy(row => row.HolderName).ThenBy(row => row.Id).Skip(offset).Take(limit);
            return Result<IReadOnlyList<AccountView>>.Success(await Project(rows).ToListAsync(ct));
        }
        catch (NpgsqlException error)
        {
            logger.LogError(error, "Falha ao listar contas");
            return Result<IReadOnlyList<AccountView>>.Failure(AccountErrors.Unavailable);
        }
    }

    private static IQueryable<AccountView> Project(IQueryable<StoredAccount> rows)
        => rows.Select(row => new AccountView(row.Id, row.HolderName,
            "***.***.***-" + row.Cpf.Substring(9, 2), row.Status, row.Version));
}
