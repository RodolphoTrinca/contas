using Contas.Domain;

namespace Contas.Application;

public interface IAccounts
{
    Task<Account?> Find(Guid id, CancellationToken cancellationToken);
    Task<bool> CpfExists(string cpf, CancellationToken cancellationToken);
    void Add(Account account);
}
