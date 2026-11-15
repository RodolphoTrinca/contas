using Contas.Domain;

namespace Contas.Api.Http.Onboarding;

internal sealed record ReviseRequest(string? HolderName, AccountStatus? Status);
