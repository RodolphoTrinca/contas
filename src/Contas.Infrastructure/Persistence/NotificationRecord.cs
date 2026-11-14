namespace Contas.Infrastructure.Persistence;

internal sealed record NotificationRecord(string Consumer, Guid EventId, string Payload);
