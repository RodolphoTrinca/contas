using Contas.Domain;

namespace Contas.Application;

public static class AccountErrors
{
    public static Error Missing => new("account.not_found", "Conta não encontrada.", ErrorKind.NotFound);
    public static Error Duplicate => new("account.duplicate_cpf", "Já existe uma conta para este CPF.", ErrorKind.Conflict);
    public static Error Unavailable => new("service.unavailable", "Serviço temporariamente indisponível.", ErrorKind.Unavailable);
}
