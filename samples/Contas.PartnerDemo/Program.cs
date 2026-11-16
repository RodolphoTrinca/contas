using Contas.PartnerDemo;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));
builder.Services.AddOptions<PartnerOptions>()
    .BindConfiguration(PartnerOptions.SectionName)
    .Validate(options => options.IsValid(),
        "Configure Partner__ClientId, Partner__Secret, Partner__TokenEndpoint, Partner__AccountsApi e Partner__ResourcePath.")
    .ValidateOnStart();

builder.Services.AddHttpClient<PartnerClient>(client => client.Timeout = TimeSpan.FromSeconds(10))
    .RemoveAllLoggers();

builder.Services.AddMessageConsumer();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.MapGet("/api/v1/accounts/{id:guid}", (Guid id, PartnerClient client, CancellationToken ct) => client.Query(id, ct));
await app.MapMessageHistory();
await app.RunAsync();
return 0;
