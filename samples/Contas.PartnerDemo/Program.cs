using Contas.PartnerDemo;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, configuration) => configuration.Enrich.FromLogContext().WriteTo.Console());
var settings = new PartnerSettings(
    builder.Configuration["Partner:ClientId"] ?? "",
    builder.Configuration["Partner:Secret"] ?? "",
    builder.Configuration["Partner:TokenEndpoint"] ?? "",
    builder.Configuration["Partner:AccountsApi"] ?? "",
    builder.Configuration["Partner:ResourcePath"] ?? "");
if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.Secret) ||
    !Uri.TryCreate(settings.TokenEndpoint, UriKind.Absolute, out _) ||
    !Uri.TryCreate(settings.AccountsApi, UriKind.Absolute, out _) ||
    !settings.ResourcePath.StartsWith("/api/v1/", StringComparison.Ordinal) ||
    !settings.ResourcePath.Contains("{id}", StringComparison.Ordinal))
{
    Console.Error.WriteLine("Configure Partner__ClientId, Partner__Secret, Partner__TokenEndpoint, Partner__AccountsApi e Partner__ResourcePath.");
    return 1;
}
builder.Services.AddSingleton(settings);
builder.Services.AddHttpClient<PartnerClient>(client => client.Timeout = TimeSpan.FromSeconds(10))
    .RemoveAllLoggers();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.MapGet("/api/v1/accounts/{id:guid}", (Guid id, PartnerClient client, CancellationToken ct) => client.Query(id, ct));
await app.RunAsync();
return 0;
