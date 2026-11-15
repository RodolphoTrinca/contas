namespace Contas.Api.Configuration;

internal sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Connection { get; set; } = "";
}
