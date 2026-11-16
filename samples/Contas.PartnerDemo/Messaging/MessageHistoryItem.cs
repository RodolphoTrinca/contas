namespace Contas.PartnerDemo;

internal sealed record MessageHistoryItem(long Sequence, Guid EventId, Guid AccountId, string EventType,
    long Version, string OccurredAt, string ReceivedAt, string? ProcessedAt, string? Outcome, string? Payload);
