using System.Text.Json;
using System.Text.Json.Serialization;
using Contas.Api.Configuration;
using Contas.Application;
using Contas.Application.Accounts.Commands;
using Contas.Domain;
using Contas.Infrastructure.Cache;
using Contas.Infrastructure.Notifications;
using Contas.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using StackExchange.Redis;

namespace Contas.Api.Startup;

internal static class ServiceRegistration
{
    internal static void ConfigureLogging(this IHostBuilder host)
    {
        host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services));
    }

    internal static void AddHttpContract(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
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
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(
            new JsonStringEnumConverter<AccountStatus>(JsonNamingPolicy.CamelCase, false)));
    }

    internal static void AddRedisCache(this IServiceCollection services)
    {
        services.AddOptions<RedisOptions>().BindConfiguration(RedisOptions.SectionName);
        services.AddSingleton<IConnectionMultiplexer>(provider =>
            ConnectionMultiplexer.Connect(provider.GetRequiredService<IOptions<RedisOptions>>().Value.Connection));
        services.AddSingleton<AccountCache>();
    }

    internal static void AddAccountServices(this IServiceCollection services)
    {
        services.AddOptions<ConnectionStringsOptions>().BindConfiguration(ConnectionStringsOptions.SectionName);
        services.AddOptions<SeedOptions>().BindConfiguration(SeedOptions.SectionName);

        services.AddDbContext<AccountsDb>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Accounts));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAccountStore, AccountStore>();
        services.AddScoped<IAccountAssignment, AccountAssignment>();
        services.AddScoped<IAccountHistory, AccountHistory>();
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(OpenAccountCommand)];
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
        services.AddScoped<DemoSeed>();
        services.AddHostedService<LocalEventDelivery>();
    }
}
