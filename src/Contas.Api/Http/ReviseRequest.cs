using Contas.Domain;

namespace Contas.Api.Http;

internal sealed record ReviseRequest(string? HolderName, AccountStatus? Status);
