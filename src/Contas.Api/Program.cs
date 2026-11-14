using Contas.Api.Http;
using Contas.Api.Security;
using Microsoft.OpenApi;
using System.Text.Json;
using System.Text.Json.Serialization;
using Contas.Application;
using Contas.Domain;
using Contas.Infrastructure.Cache;
using Contas.Infrastructure.Notifications;
using Contas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, services, configuration) => configuration
    .ReadFrom.Services(services).Enrich.FromLogContext()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console());
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
        };
        document.Security = [new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        }];
        return Task.CompletedTask;
    });
});
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(
    new JsonStringEnumConverter<AccountStatus>(JsonNamingPolicy.CamelCase, false)));
builder.Services.AddDbContext<AccountsDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Accounts")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"] ?? "127.0.0.1:16379,abortConnect=false"));
builder.Services.AddSingleton<AccountCache>();
builder.Services.AddScoped<IAccountStore, AccountStore>();
builder.Services.AddScoped<IAccountAssignment, AccountAssignment>();
builder.Services.AddScoped<OpenAccount>();
builder.Services.AddScoped<ReviseAccount>();
builder.Services.AddScoped<DeleteAccount>();
builder.Services.AddScoped<GetAccount>();
builder.Services.AddScoped<ListAccounts>();
builder.Services.AddScoped<DemoSeed>();
builder.Services.AddHostedService<LocalEventDelivery>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.Authority = builder.Configuration["Auth:Issuer"];
    if (builder.Configuration["Auth:Metadata"] is { } metadata) options.MetadataAddress = metadata;
    options.RequireHttpsMetadata = builder.Configuration.GetValue("Auth:RequireHttps", true);
    options.Audience = "contas-api";
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("operator", policy => policy.RequireAuthenticatedUser().RequireClaim("azp", "contas-web")
        .RequireAssertion(context => TokenRoles.Has(context.User, "accounts-operator")))
    .AddPolicy("account-read", policy => policy.RequireAuthenticatedUser().RequireClaim("azp", "contas-web")
        .RequireAssertion(context => TokenRoles.Has(context.User, "accounts-operator") ||
            (Guid.TryParse(context.User.FindFirst("sub")?.Value, out _) &&
            (TokenRoles.Has(context.User, "accounts-manager") || TokenRoles.Has(context.User, "accounts-customer")))))
    .AddPolicy("account-write", policy => policy.RequireAuthenticatedUser().RequireClaim("azp", "contas-web")
        .RequireAssertion(context => TokenRoles.Has(context.User, "accounts-operator") ||
            (Guid.TryParse(context.User.FindFirst("sub")?.Value, out _) &&
            TokenRoles.Has(context.User, "accounts-manager"))))
    .AddPolicy("fraud", policy => policy.RequireAuthenticatedUser().RequireClaim("azp", "fraud-api")
        .RequireAssertion(context => TokenRoles.Has(context.User, "fraud-reader")))
    .AddPolicy("cards", policy => policy.RequireAuthenticatedUser().RequireClaim("azp", "cards-api")
        .RequireAssertion(context => TokenRoles.Has(context.User, "cards-reader")));

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountsDb>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Npgsql.NpgsqlException error)
    {
        app.Logger.LogCritical(error, "Falha ao aplicar a migração de contas");
        Environment.ExitCode = 1;
        return;
    }
    if (app.Configuration.GetValue<bool>("Seed:Enabled") &&
        !await scope.ServiceProvider.GetRequiredService<DemoSeed>().Run(CancellationToken.None))
    {
        Environment.ExitCode = 1;
        return;
    }
}

app.UseSerilogRequestLogging();
app.UseWhen(http => http.Request.Path.StartsWithSegments("/api"), branch =>
{
    branch.UseExceptionHandler(errorApp => errorApp.Run(http =>
        Responses.Fail(http, 500, "service.internal_error", "Falha interna.").ExecuteAsync(http)));
    branch.UseStatusCodePages(context => Responses.Status(context.HttpContext));
});
app.UseAuthentication();
app.UseAuthorization();
app.MapV1();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/health", () => Results.Ok()).ExcludeFromDescription();
await app.RunAsync();
