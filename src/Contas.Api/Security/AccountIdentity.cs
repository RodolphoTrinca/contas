using System.Security.Claims;
using Contas.Application;

namespace Contas.Api.Security;

internal static class AccountIdentity
{
    internal static AccountAccess? Access(ClaimsPrincipal principal)
        => TokenRoles.Has(principal, KeycloakRoles.Operator) ? null :
            new(Guid.TryParse(principal.FindFirstValue(TokenClaimNames.Subject), out var subject) ? subject.ToString() : "",
                TokenRoles.Has(principal, KeycloakRoles.Manager));

    internal static bool HasSubject(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(TokenClaimNames.Subject), out var subject) && subject != Guid.Empty;

    internal static string Scope(ClaimsPrincipal principal)
        => (principal.FindFirstValue(TokenClaimNames.AuthorizedParty) ?? "") + ":" + (principal.FindFirstValue(TokenClaimNames.Subject) ?? "");
}
