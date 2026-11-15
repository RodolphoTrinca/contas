using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Queries;

public sealed class GetAccountHandler(IAccountStore store)
    : IQueryHandler<GetAccountQuery, Result<AccountView>>
{
    public ValueTask<Result<AccountView>> Handle(GetAccountQuery query, CancellationToken ct)
        => new(store.Get(query.Id, ct, query.Access));
}
