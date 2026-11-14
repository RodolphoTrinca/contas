using System.Security.Claims;

namespace Contas.Api.Security;

internal static class AccountIdentity
{
    internal static string Scope(ClaimsPrincipal principal)
        => (principal.FindFirstValue("azp") ?? "") + ":" + (principal.FindFirstValue("sub") ?? "");
}
