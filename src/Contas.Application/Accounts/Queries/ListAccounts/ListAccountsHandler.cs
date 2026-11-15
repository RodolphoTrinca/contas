using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Queries;

public sealed class ListAccountsHandler(IAccountStore store)
    : IQueryHandler<ListAccountsQuery, Result<IReadOnlyList<AccountView>>>
{
    public ValueTask<Result<IReadOnlyList<AccountView>>> Handle(ListAccountsQuery query, CancellationToken ct)
    {
        if (query.Limit is < 1 or > 100 || query.Offset is < 0)
            return ValueTask.FromResult(Result<IReadOnlyList<AccountView>>.Failure(
                new("account.invalid_pagination", "Informe limit entre 1 e 100 e offset maior ou igual a zero.",
                    ErrorKind.Validation)));

        return new(store.List(query.Limit ?? 25, query.Offset ?? 0, ct, query.Access));
    }
}
