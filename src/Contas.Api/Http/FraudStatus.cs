using Contas.Domain;

namespace Contas.Api.Http;

internal sealed record FraudStatus(Guid Id, AccountStatus Status);
