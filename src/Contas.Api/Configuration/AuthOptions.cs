namespace Contas.Api.Configuration;

internal sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string? Issuer { get; set; }
    public string? Metadata { get; set; }
    public bool RequireHttps { get; set; } = true;
}
