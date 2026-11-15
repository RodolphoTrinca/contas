using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Queries;

public sealed record ListAccountsQuery(int? Limit, int? Offset, AccountAccess? Access = null) : IQuery<Result<IReadOnlyList<AccountView>>>;
