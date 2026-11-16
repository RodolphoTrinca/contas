namespace Contas.PartnerDemo;

internal sealed record AccountNotification(Guid EventId, Guid AccountId, string Type, long Version,
    int Status, DateTimeOffset OccurredAt)
{
    internal bool IsValid() => EventId != Guid.Empty && AccountId != Guid.Empty && Version > 0 &&
        OccurredAt != default && Status is 0 or 1 && Type is "AccountCreated" or "AccountUpdated" or "AccountDeleted";

    internal string Simulate(string consumer) => (consumer, Type, Status) switch
    {
        ("fraud", "AccountDeleted", _) => "Conta retirada da análise",
        ("fraud", _, 0) => "Conta ativa sinalizada para análise",
        ("fraud", _, _) => "Conta inativa registrada",
        ("cards", "AccountDeleted", _) => "Elegibilidade encerrada",
        ("cards", _, 0) => "Conta elegível para avaliação de cartões",
        _ => "Conta inelegível por inatividade"
    };
}
