using System.Text.Json;
using Contas.Domain;
using Contas.Infrastructure.Cache;
using Contas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Contas.Infrastructure.Notifications;

public sealed class LocalEventDelivery(IServiceScopeFactory scopes, AccountCache cache, ILogger<LocalEventDelivery> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await Deliver(stoppingToken); }
            catch (Exception error) when (error is NpgsqlException or DbUpdateException or JsonException)
            {
                logger.LogWarning(error, "A entrega local será repetida");
            }
        }
    }

    private async Task Deliver(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountsDb>();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(2103371234)", ct);
        await db.Receipts.Where(row => row.ExpiresAt <= DateTimeOffset.UtcNow)
            .ExecuteDeleteAsync(ct);
        var batch = await db.Outbox.Where(row => !row.Published).OrderBy(row => row.Sequence).Take(100).ToListAsync(ct);
        foreach (var row in batch)
        {
            var change = JsonSerializer.Deserialize<AccountEvent>(row.Payload);
            if (change is null) break;
            if (!await cache.Invalidate(change.AccountId)) continue;
            db.Inbox.Add(new("fraud", row.EventId, row.Payload));
            db.Inbox.Add(new("cards", row.EventId, row.Payload));
            row.Published = true;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
