using Contas.Domain;
using Mediator;

namespace Contas.Application.Cards.Queries;

public sealed record GetCardEligibilityQuery(AccountId AccountId) : IQuery<Result<CardEligibility>>;
