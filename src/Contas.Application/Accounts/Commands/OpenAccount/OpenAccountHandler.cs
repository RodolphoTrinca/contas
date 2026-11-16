using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class OpenAccountHandler(IAccountStore store, TimeProvider clock)
    : ICommandHandler<OpenAccountCommand, Result<WriteReceipt>>
{
    public ValueTask<Result<WriteReceipt>> Handle(OpenAccountCommand command, CancellationToken ct)
    {
        var (scope, key, name, cpf, access) = command;
        var operation = new IdempotentWrite(scope, key, Fingerprint.For(new { Operation = "open", name, cpf }));
        return new(store.Write(operation, async accounts =>
            {
                var result = Account.Open(name, cpf, clock.GetUtcNow(), access is { IsManager: true } ? access.Subject : null);
                if (!result.IsSuccess) return result;
                if (await accounts.CpfExists(result.Value.Cpf, ct))
                    return Result<Account>.Failure(AccountErrors.Duplicate);
                accounts.Add(result.Value);
                return result;
            }, ct, access));
    }
}
