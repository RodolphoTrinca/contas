using Contas.Domain;

namespace Contas.Application;

public interface IAccounts
{
    Task<Account?> Find(AccountId id, CancellationToken cancellationToken);
    Task<bool> CpfExists(Cpf cpf, CancellationToken cancellationToken);
    void Add(Account account);
}
