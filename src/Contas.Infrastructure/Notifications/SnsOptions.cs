namespace Contas.Infrastructure.Notifications;

public sealed class SnsOptions
{
    public const string SectionName = "Messaging";

    public string? TopicArn { get; set; }
    public string? Endpoint { get; set; }
    public string Region { get; set; } = "us-east-1";
}
