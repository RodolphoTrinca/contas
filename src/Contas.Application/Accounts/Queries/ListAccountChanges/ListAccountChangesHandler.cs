using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Queries;

public sealed class ListAccountChangesHandler(IAccountHistory history)
    : IQueryHandler<ListAccountChangesQuery, Result<AccountChangePage>>
{
    public ValueTask<Result<AccountChangePage>> Handle(ListAccountChangesQuery query, CancellationToken ct)
    {
        if (query.Limit is < 1 or > 100 || query.Before is <= 0 || query.AccountId == Guid.Empty)
            return ValueTask.FromResult(Result<AccountChangePage>.Failure(
                new("audit.invalid_filter", "Informe um ID de conta válido, um cursor positivo e um limite de 1 a 100.",
                    ErrorKind.Validation)));

        return new(history.List(query.AccountId, query.Before, query.Limit ?? 25, ct));
    }
}
