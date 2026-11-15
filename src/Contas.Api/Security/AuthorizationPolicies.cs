namespace Contas.Api.Security;

internal static class AuthorizationPolicies
{
    internal const string Operator = "operator";
    internal const string AccountRead = "account-read";
    internal const string AccountWrite = "account-write";
    internal const string Fraud = "fraud";
    internal const string Cards = "cards";
}
