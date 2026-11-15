namespace Contas.PartnerDemo;

internal sealed class PartnerOptions
{
    public const string SectionName = "Partner";

    public string ClientId { get; set; } = "";
    public string Secret { get; set; } = "";
    public string TokenEndpoint { get; set; } = "";
    public string AccountsApi { get; set; } = "";
    public string ResourcePath { get; set; } = "";

    internal bool IsValid() =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(Secret) &&
        Uri.TryCreate(TokenEndpoint, UriKind.Absolute, out _) &&
        Uri.TryCreate(AccountsApi, UriKind.Absolute, out _) &&
        ResourcePath.StartsWith("/api/v1/", StringComparison.Ordinal) &&
        ResourcePath.Contains("{id}", StringComparison.Ordinal);
}
