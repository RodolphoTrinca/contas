using System.ComponentModel;
namespace Contas.Api.Http.Onboarding;

internal sealed record OpenRequest(
    [property: Description("Nome do titular, com 2 a 120 caracteres após trim.")] string? HolderName,
    [property: Description("CPF válido de 11 dígitos ou pontuado. Deve ser único entre contas não excluídas.")] string? Cpf);
