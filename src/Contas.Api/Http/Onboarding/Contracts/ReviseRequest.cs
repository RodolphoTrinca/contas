using System.ComponentModel;
using Contas.Domain;

namespace Contas.Api.Http.Onboarding;

internal sealed record ReviseRequest(
    [property: Description("Novo nome, com 2 a 120 caracteres após trim.")] string? HolderName,
    [property: Description("Status desejado, active ou inactive.")] AccountStatus? Status);
