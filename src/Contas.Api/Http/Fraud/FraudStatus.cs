using Contas.Domain;

namespace Contas.Api.Http.Fraud;

internal sealed record FraudStatus(Guid Id, AccountStatus Status);
