namespace Contas.Api.Configuration;

internal sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    public string? Accounts { get; set; }
}
