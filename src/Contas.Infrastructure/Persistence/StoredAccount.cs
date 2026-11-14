using Contas.Domain;

namespace Contas.Infrastructure.Persistence;

internal sealed class StoredAccount
{
    public string? ManagerSubject { get; set; }
    public string? OwnerSubject { get; set; }
    public Guid Id { get; init; }
    public string HolderName { get; set; } = "";
    public string Cpf { get; init; } = "";
    public AccountStatus Status { get; set; }
    public long Version { get; set; }
    public bool Deleted { get; set; }
}
