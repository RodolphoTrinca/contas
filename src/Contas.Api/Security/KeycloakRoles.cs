namespace Contas.Api.Security;

internal static class KeycloakRoles
{
    internal const string Operator = "accounts-operator";
    internal const string Manager = "accounts-manager";
    internal const string Customer = "accounts-customer";
    internal const string FraudReader = "fraud-reader";
    internal const string CardsReader = "cards-reader";
}
