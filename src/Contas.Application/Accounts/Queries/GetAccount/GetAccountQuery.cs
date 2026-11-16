using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Queries;

public sealed record GetAccountQuery(AccountId Id, AccountAccess? Access = null) : IQuery<Result<AccountView>>;
