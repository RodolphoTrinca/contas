using System.Text.Json;
using Contas.Application;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Contas.Infrastructure.Cache;

public sealed class AccountCache(IConnectionMultiplexer redis, TimeProvider clock, ILogger<AccountCache> logger)
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public async Task<CacheLookup> Read(Guid id)
    {
        try
        {
            var result = await redis.GetDatabase().ScriptEvaluateAsync("""
                return {redis.call('GET', KEYS[1]) or '', redis.call('GET', KEYS[2]) or ''}
                """, [Key(id, "generation"), Key(id, "view")]);
            var values = (RedisResult[]?)result;
            if (values is not { Length: 2 }) return new(null, null);
            var view = (string?)values[1];
            return new((string?)values[0], string.IsNullOrEmpty(view) ? null : JsonSerializer.Deserialize<AccountView>(view));
        }
        catch (Exception error) when (error is RedisException or JsonException)
        {
            logger.LogWarning(error, "Cache de contas indisponível");
            return new(null, null);
        }
    }

    public async Task Fill(Guid id, string? generation, AccountView view)
    {
        if (generation is null) return;
        var now = clock.GetUtcNow();
        var tomorrow = TimeZoneInfo.ConvertTime(now, Zone).Date.AddDays(1);
        var ttl = new DateTimeOffset(tomorrow, Zone.GetUtcOffset(tomorrow)) - now;
        try
        {
            await redis.GetDatabase().ScriptEvaluateAsync("""
                if (redis.call('GET', KEYS[1]) or '') == ARGV[1] then
                    redis.call('SET', KEYS[2], ARGV[2], 'PX', ARGV[3])
                end
                """, [Key(id, "generation"), Key(id, "view")], [generation, JsonSerializer.Serialize(view), Math.Max(1, (long)ttl.TotalMilliseconds)]);
        }
        catch (RedisException error) { logger.LogWarning(error, "Falha ao preencher cache"); }
    }

    public async Task<bool> Invalidate(Guid id)
    {
        try
        {
            await redis.GetDatabase().ScriptEvaluateAsync("""
                redis.call('SET', KEYS[1], ARGV[1], 'EX', 172800)
                redis.call('DEL', KEYS[2])
                """, [Key(id, "generation"), Key(id, "view")], [Guid.NewGuid().ToString()]);
            return true;
        }
        catch (RedisException error)
        {
            logger.LogWarning(error, "A invalidação do cache será repetida pela outbox");
            return false;
        }
    }

    private static RedisKey Key(Guid id, string suffix) => $"accounts:{{{id}}}:{suffix}";
}
