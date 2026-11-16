namespace Contas.PartnerDemo;

internal sealed class ConsumerOptions
{
    public const string SectionName = "Messaging";
    public string? QueueUrl { get; set; }
    public string? Endpoint { get; set; }
    public string Region { get; set; } = "us-east-1";
    public string Consumer { get; set; } = "";
    public string DatabasePath { get; set; } = "";

    internal bool IsValid() => QueueUrl is null ||
        (Uri.TryCreate(QueueUrl, UriKind.Absolute, out _) && Consumer is "fraud" or "cards" &&
         !string.IsNullOrWhiteSpace(DatabasePath));
}
