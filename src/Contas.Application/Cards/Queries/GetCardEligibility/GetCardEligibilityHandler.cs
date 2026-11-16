using Contas.Domain;
using Mediator;

namespace Contas.Application.Cards.Queries;

public sealed class GetCardEligibilityHandler(IAccountStore accounts)
    : IQueryHandler<GetCardEligibilityQuery, Result<CardEligibility>>
{
    public async ValueTask<Result<CardEligibility>> Handle(GetCardEligibilityQuery query, CancellationToken ct)
    {
        var result = await accounts.Get(query.AccountId, ct);
        if (!result.IsSuccess) return Result<CardEligibility>.Failure(result.Error);

        var account = result.Value;
        return Result<CardEligibility>.Success(new(account.Id, account.Status == AccountStatus.Active));
    }
}
