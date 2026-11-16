using System.ComponentModel;
namespace Contas.Api.Http.Onboarding;

internal sealed record AssignmentRequest(
    [property: Description("UUID da identidade Keycloak do gerente ou titular, conforme a operação.")] Guid Subject);
