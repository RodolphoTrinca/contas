namespace Contas.PartnerDemo;

internal sealed record PartnerSettings(string ClientId, string Secret, string TokenEndpoint, string AccountsApi, string ResourcePath);
