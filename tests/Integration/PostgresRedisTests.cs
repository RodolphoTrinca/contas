using Contas.Application;
using Contas.Application.Accounts.Commands;
using Contas.Application.Accounts.Queries;
using Contas.Domain;
using Contas.Infrastructure.Cache;
using Contas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Contas.Infrastructure.Notifications;
using Npgsql;
using StackExchange.Redis;
using Xunit;

public sealed class PostgresRedisTests
{
    [Fact]
    public async Task ConcurrentReplayCommitsOneAccountAndOneEventAndInvalidatesRedis()
    {
        var connection = Environment.GetEnvironmentVariable("CONTAS_TEST_POSTGRES");
        var redisConnection = Environment.GetEnvironmentVariable("CONTAS_TEST_REDIS");
        Assert.False(string.IsNullOrEmpty(connection), "Execute scripts/test.sh integration para criar o PostgreSQL descartável.");
        Assert.False(string.IsNullOrEmpty(redisConnection), "Informe CONTAS_TEST_REDIS.");
        var options = new DbContextOptionsBuilder<AccountsDb>().UseNpgsql(connection).Options;
        await using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnection!);
        var cache = new AccountCache(redis, TimeProvider.System, NullLogger<AccountCache>.Instance);
        var key = Guid.NewGuid();
        var scope = Guid.NewGuid().ToString();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var ct = timeout.Token;
        await using (var setup = new AccountsDb(options))
            await setup.Database.MigrateAsync(ct);

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = new AccountsDb(options);
            return await new OpenAccountHandler(Store(db), TimeProvider.System)
                .Handle(new OpenAccountCommand(scope, key, "Conta Integração", "52998224725"), ct);
        }));
        Assert.All(results, result => Assert.True(result.IsSuccess, result.Error?.Code));
        Assert.Single(results, result => result.Value is { Replayed: false });
        var account = Assert.Single(results.Select(result => result.Value!.Account).Distinct());
        var accountId = new AccountId(account.Id);
        var initialVersion = AccountVersion.Create(account.Version).Value!;
        await using var readDb = new AccountsDb(options);
        Assert.Equal(account, (await Store(readDb).Get(accountId, ct)).Value);
        Assert.Equal(account, (await cache.Read(account.Id)).Account);

        await using (var writeDb = new AccountsDb(options))
        {
            var changed = await new ReviseAccountHandler(Store(writeDb), TimeProvider.System)
                .Handle(new ReviseAccountCommand(scope, Guid.NewGuid(), accountId, initialVersion, "Conta Atualizada",
                    AccountStatus.Inactive), ct);
            Assert.True(changed.IsSuccess);
        }
        Assert.Null((await cache.Read(account.Id)).Account);
        Assert.Equal(AccountStatus.Inactive, (await Store(readDb).Get(accountId, ct)).Value!.Status);

        await using (var reopenedDb = new AccountsDb(options))
        {
            var replay = await new OpenAccountHandler(Store(reopenedDb), TimeProvider.System)
                .Handle(new OpenAccountCommand(scope, key, "Conta Integração", "52998224725"), ct);
            Assert.Equal(account, replay.Value?.Account);
            Assert.True(replay.Value?.Replayed);
            var conflict = await new OpenAccountHandler(Store(reopenedDb), TimeProvider.System)
                .Handle(new OpenAccountCommand(scope, key, "Outra Conta", "52998224725"), ct);
            Assert.Equal("idempotency.key_conflict", conflict.Error?.Code);
        }
        await using var sql = new NpgsqlConnection(connection);
        await sql.OpenAsync(ct);
        await using var events = new NpgsqlCommand("""SELECT count(*) FROM "Outbox" WHERE "Payload"::jsonb ->> 'Type' = 'AccountCreated'""", sql);
        Assert.Equal(1L, await events.ExecuteScalarAsync(ct));
        await using var eventPayload = new NpgsqlCommand("""
            SELECT "Payload" FROM "Outbox" WHERE "Payload"::jsonb ->> 'Type' = 'AccountCreated'
            """, sql);
        var serialized = Assert.IsType<string>(await eventPayload.ExecuteScalarAsync(ct));
        using var payload = System.Text.Json.JsonDocument.Parse(serialized);
        Assert.Equal(account.Id, payload.RootElement.GetProperty("AccountId").GetGuid());
        Assert.Equal(account.Version, payload.RootElement.GetProperty("Version").GetInt64());
        var services = new ServiceCollection();
        services.AddDbContext<AccountsDb>(builder => builder.UseNpgsql(connection));
        await using var provider = services.BuildServiceProvider();
        using var worker = new LocalEventDelivery(provider.GetRequiredService<IServiceScopeFactory>(),
            cache, NullLogger<LocalEventDelivery>.Instance);
        await worker.StartAsync(ct);
        try
        {
            await using var delivered = new NpgsqlCommand("""SELECT count(*) FROM "Inbox" WHERE "Consumer" IN ('fraud', 'cards')""", sql);
            for (var attempt = 0; attempt < 20; attempt++)
            {
                if (Equals(4L, await delivered.ExecuteScalarAsync(ct))) break;
                await Task.Delay(250, ct);
            }
            Assert.Equal(4L, await delivered.ExecuteScalarAsync(ct));
        }
        finally { await worker.StopAsync(CancellationToken.None); }

        var operatorActor = $"contas-web:{Guid.NewGuid()}";
        await using (var assignmentDb = new AccountsDb(options))
        {
            var assignment = new AccountAssignment(assignmentDb, TimeProvider.System, NullLogger<AccountAssignment>.Instance);
            var manager = AccountSubject.Create(Guid.NewGuid()).Value!;
            Assert.True((await assignment.Transfer(accountId, manager, operatorActor, ct)).IsSuccess);
            Assert.True((await assignment.Transfer(accountId, manager, operatorActor, ct)).IsSuccess);
            Assert.True((await assignment.LinkOwner(accountId, AccountSubject.Create(Guid.NewGuid()).Value!, operatorActor, ct)).IsSuccess);
        }
        await using (var deleteDb = new AccountsDb(options))
        {
            var deleted = await new DeleteAccountHandler(Store(deleteDb), TimeProvider.System)
                .Handle(new DeleteAccountCommand(operatorActor, Guid.NewGuid(), accountId,
                    AccountVersion.Create(account.Version + 1).Value!), ct);
            Assert.True(deleted.IsSuccess);
        }

        await using var audit = new NpgsqlCommand("""
            SELECT "Action", "Actor", "OccurredAt"
            FROM audit.account_changes
            WHERE "AccountId" = $1
            ORDER BY "Id"
            """, sql);
        audit.Parameters.AddWithValue(account.Id);
        await using var rows = await audit.ExecuteReaderAsync(ct);
        foreach (var (action, actor) in new[]
        {
            ("AccountCreated", scope), ("AccountUpdated", scope),
            ("PortfolioAssigned", operatorActor), ("OwnerLinked", operatorActor),
            ("AccountDeleted", operatorActor)
        })
        {
            Assert.True(await rows.ReadAsync(ct));
            Assert.Equal(action, rows.GetString(0));
            Assert.Equal(actor, rows.GetString(1));
            Assert.False(rows.IsDBNull(2));
        }
        Assert.False(await rows.ReadAsync(ct));

        var history = new ListAccountChangesHandler(new AccountHistory(readDb, NullLogger<AccountHistory>.Instance));
        var firstPage = await history.Handle(new ListAccountChangesQuery(account.Id, null, 2), ct);
        Assert.True(firstPage.IsSuccess);
        Assert.Equal(new[] { "AccountDeleted", "OwnerLinked" }, firstPage.Value!.Items.Select(item => item.Action));

        Assert.True(long.TryParse(firstPage.Value.NextBefore, out var cursor));
        var nextPage = await history.Handle(new ListAccountChangesQuery(account.Id, cursor, 100), ct);
        Assert.True(nextPage.IsSuccess);
        Assert.Equal(new[] { "PortfolioAssigned", "AccountUpdated", "AccountCreated" },
            nextPage.Value!.Items.Select(item => item.Action));
        Assert.Null(nextPage.Value.NextBefore);

        AccountStore Store(AccountsDb db) => new(db, cache, TimeProvider.System, NullLogger<AccountStore>.Instance);
    }
}
