using System.Globalization;
using Contas.Application;
using Contas.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Contas.Infrastructure.Persistence;

public sealed class AccountHistory(AccountsDb db, ILogger<AccountHistory> logger) : IAccountHistory
{
    public async Task<Result<AccountChangePage>> List(Guid? accountId, long? before, int limit, CancellationToken ct)
    {
        try
        {
            var query = db.AuditEntries.AsNoTracking();
            if (accountId is { } id) query = query.Where(row => row.AccountId == id);
            if (before is { } cursor) query = query.Where(row => row.Id < cursor);
            var rows = await query.OrderByDescending(row => row.Id).Take(limit + 1).ToListAsync(ct);
            var items = rows.Take(limit).Select(row => new AccountChangeView(
                Cursor(row.Id), row.AccountId, row.Action, row.Actor, row.OccurredAt)).ToArray();
            return Result<AccountChangePage>.Success(new(items, rows.Count > limit ? items[^1].Id : null));
        }
        catch (NpgsqlException error)
        {
            logger.LogError(error, "Falha ao consultar o histórico de contas");
            return Result<AccountChangePage>.Failure(AccountErrors.Unavailable);
        }
    }

    private static string Cursor(long id) => id.ToString(CultureInfo.InvariantCulture);
}
