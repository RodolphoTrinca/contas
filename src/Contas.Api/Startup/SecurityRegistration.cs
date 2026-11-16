using Microsoft.AspNetCore.Authorization;
using Contas.Api.Configuration;
using Contas.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Contas.Api.Startup;

internal static class SecurityRegistration
{
    internal static void AddAccountSecurity(this IServiceCollection services)
    {
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((options, configuredAuth) =>
            {
                var auth = configuredAuth.Value;
                options.MapInboundClaims = false;
                options.Authority = auth.Issuer;
                if (auth.Metadata is { } metadata) options.MetadataAddress = metadata;
                options.RequireHttpsMetadata = auth.RequireHttps;
                options.Audience = KeycloakClients.AccountsApi;
            });

        AddAuthorizationPolicies(services);
    }

    private static void AddAuthorizationPolicies(IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Operator, ConfigureOperator)
            .AddPolicy(AuthorizationPolicies.AccountRead, ConfigureAccountRead)
            .AddPolicy(AuthorizationPolicies.AccountWrite, ConfigureAccountWrite)
            .AddPolicy(AuthorizationPolicies.Fraud, ConfigureFraud)
            .AddPolicy(AuthorizationPolicies.Cards, ConfigureCards);
    }

    private static void ConfigureOperator(AuthorizationPolicyBuilder policy)
        => RequireClient(policy, KeycloakClients.Web)
            .RequireAssertion(context => AccountIdentity.HasSubject(context.User) &&
                TokenRoles.Has(context.User, KeycloakRoles.Operator));

    private static void ConfigureAccountRead(AuthorizationPolicyBuilder policy)
        => RequireClient(policy, KeycloakClients.Web)
            .RequireAssertion(context => TokenRoles.Has(context.User, KeycloakRoles.Operator) ||
                (Guid.TryParse(context.User.FindFirst(TokenClaimNames.Subject)?.Value, out _) &&
                 (TokenRoles.Has(context.User, KeycloakRoles.Manager) ||
                  TokenRoles.Has(context.User, KeycloakRoles.Customer))));

    private static void ConfigureAccountWrite(AuthorizationPolicyBuilder policy)
        => RequireClient(policy, KeycloakClients.Web)
            .RequireAssertion(context => AccountIdentity.HasSubject(context.User) &&
                (TokenRoles.Has(context.User, KeycloakRoles.Operator) ||
                 TokenRoles.Has(context.User, KeycloakRoles.Manager)));

    private static void ConfigureFraud(AuthorizationPolicyBuilder policy)
        => RequireClient(policy, KeycloakClients.FraudApi)
            .RequireAssertion(context => TokenRoles.Has(context.User, KeycloakRoles.FraudReader));

    private static void ConfigureCards(AuthorizationPolicyBuilder policy)
        => RequireClient(policy, KeycloakClients.CardsApi)
            .RequireAssertion(context => TokenRoles.Has(context.User, KeycloakRoles.CardsReader));

    private static AuthorizationPolicyBuilder RequireClient(AuthorizationPolicyBuilder policy, string client)
        => policy.RequireAuthenticatedUser().RequireClaim(TokenClaimNames.AuthorizedParty, client);
}
