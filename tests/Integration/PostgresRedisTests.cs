using Contas.Application;
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
            return await new OpenAccount(Store(db), TimeProvider.System)
                .Execute(scope, key, "Conta Integração", "52998224725", ct);
        }));
        Assert.All(results, result => Assert.True(result.IsSuccess, result.Error?.Code));
        Assert.Single(results, result => result.Value is { Replayed: false });
        var account = Assert.Single(results.Select(result => result.Value!.Account).Distinct());
        await using var readDb = new AccountsDb(options);
        Assert.Equal(account, (await Store(readDb).Get(account.Id, ct)).Value);
        Assert.Equal(account, (await cache.Read(account.Id)).Account);

        await using (var writeDb = new AccountsDb(options))
        {
            var changed = await new ReviseAccount(Store(writeDb), TimeProvider.System)
                .Execute(scope, Guid.NewGuid(), account.Id, "Conta Atualizada", AccountStatus.Inactive, ct);
            Assert.True(changed.IsSuccess);
        }
        Assert.Null((await cache.Read(account.Id)).Account);
        Assert.Equal(AccountStatus.Inactive, (await Store(readDb).Get(account.Id, ct)).Value!.Status);

        await using (var reopenedDb = new AccountsDb(options))
        {
            var replay = await new OpenAccount(Store(reopenedDb), TimeProvider.System)
                .Execute(scope, key, "Conta Integração", "52998224725", ct);
            Assert.Equal(account, replay.Value?.Account);
            Assert.True(replay.Value?.Replayed);
            var conflict = await new OpenAccount(Store(reopenedDb), TimeProvider.System)
                .Execute(scope, key, "Outra Conta", "52998224725", ct);
            Assert.Equal("idempotency.key_conflict", conflict.Error?.Code);
        }
        await using var sql = new NpgsqlConnection(connection);
        await sql.OpenAsync(ct);
        await using var events = new NpgsqlCommand("""SELECT count(*) FROM "Outbox" WHERE "Payload"::jsonb ->> 'Type' = 'AccountCreated'""", sql);
        Assert.Equal(1L, await events.ExecuteScalarAsync(ct));
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

        AccountStore Store(AccountsDb db) => new(db, cache, TimeProvider.System, NullLogger<AccountStore>.Instance);
    }
}
