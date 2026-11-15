using Contas.Api.Configuration;
using Contas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Contas.Api.Startup;

internal static class DatabaseMigrations
{
    internal static async Task<bool> ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AccountsDb>();
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (NpgsqlException error)
        {
            app.Logger.LogCritical(error, "Falha ao aplicar a migração de contas");
            return false;
        }

        var seed = services.GetRequiredService<IOptions<SeedOptions>>().Value;
        return !seed.Enabled || await services.GetRequiredService<DemoSeed>().Run(CancellationToken.None);
    }
}
