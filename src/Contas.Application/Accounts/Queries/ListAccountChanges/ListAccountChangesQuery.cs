using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Queries;

public sealed record ListAccountChangesQuery(Guid? AccountId, long? Before, int? Limit) : IQuery<Result<AccountChangePage>>;
