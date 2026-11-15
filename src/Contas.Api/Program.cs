using Contas.Api.Startup;

var migrate = args is ["--migrate", ..];
var builder = WebApplication.CreateBuilder(migrate ? args[1..] : args);

builder.Host.ConfigureLogging();
builder.Services.AddHttpContract();
builder.Services.AddRedisCache();
builder.Services.AddAccountServices();
builder.Services.AddAccountSecurity();

var app = builder.Build();
if (migrate)
{
    await using var migrationHost = app;
    return await migrationHost.ApplyDatabaseMigrationsAsync() ? 0 : 1;
}

app.ConfigureRequestPipeline();
app.MapApplicationEndpoints();

await app.RunAsync();
return 0;
