using System.Security.Claims;
using Contas.Application;

namespace Contas.Api.Security;

internal static class AccountIdentity
{
    internal static AccountAccess? Access(ClaimsPrincipal principal)
        => TokenRoles.Has(principal, "accounts-operator") ? null :
            new(Guid.TryParse(principal.FindFirstValue("sub"), out var subject) ? subject.ToString() : "",
                TokenRoles.Has(principal, "accounts-manager"));

    internal static string Scope(ClaimsPrincipal principal)
        => (principal.FindFirstValue("azp") ?? "") + ":" + (principal.FindFirstValue("sub") ?? "");
}
