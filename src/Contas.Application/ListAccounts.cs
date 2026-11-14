using Contas.Domain;

namespace Contas.Application;

public sealed class ListAccounts(IAccountStore store)
{
    public Task<Result<IReadOnlyList<AccountView>>> Execute(int? limit, int? offset, CancellationToken ct)
        => limit is < 1 or > 100 || offset is < 0
            ? Task.FromResult(Result<IReadOnlyList<AccountView>>.Failure(
                new("account.invalid_pagination", "Informe limit entre 1 e 100 e offset maior ou igual a zero.", ErrorKind.Validation)))
            : store.List(limit ?? 25, offset ?? 0, ct);
}
