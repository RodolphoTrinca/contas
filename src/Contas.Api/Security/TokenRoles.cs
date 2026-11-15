using System.Security.Claims;
using System.Text.Json;

namespace Contas.Api.Security;

internal static class TokenRoles
{
    private const string RolesProperty = "roles";

    internal static bool Has(ClaimsPrincipal user, string role)
    {
        if (user.FindFirst(TokenClaimNames.RealmAccess)?.Value is not { } access) return false;
        try
        {
            using var json = JsonDocument.Parse(access);
            return json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty(RolesProperty, out var roles) && roles.ValueKind == JsonValueKind.Array &&
                roles.EnumerateArray().Any(value => value.ValueKind == JsonValueKind.String && value.GetString() == role);
        }
        catch (JsonException) { return false; }
    }
}
