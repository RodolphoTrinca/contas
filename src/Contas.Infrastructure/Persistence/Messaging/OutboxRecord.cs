namespace Contas.Infrastructure.Persistence;

internal sealed class OutboxRecord
{
    public long Sequence { get; init; }
    public Guid EventId { get; init; }
    public string Payload { get; init; } = "";
    public bool Published { get; set; }
}
