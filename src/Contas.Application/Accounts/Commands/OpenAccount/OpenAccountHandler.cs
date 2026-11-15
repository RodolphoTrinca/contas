using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class OpenAccountHandler(IAccountStore store, TimeProvider clock)
    : ICommandHandler<OpenAccountCommand, Result<WriteReceipt>>
{
    public ValueTask<Result<WriteReceipt>> Handle(OpenAccountCommand command, CancellationToken ct)
    {
        var (scope, key, name, cpf, access) = command;
        return new(store.Write(scope, key, Fingerprint.For(new { Operation = "open", name, cpf }), async accounts =>
            {
                var result = Account.Open(name, cpf, clock.GetUtcNow());
                if (!result.IsSuccess) return result;
                if (await accounts.CpfExists(result.Value.Cpf.Value, ct))
                    return Result<Account>.Failure(AccountErrors.Duplicate);
                accounts.Add(result.Value);
                return result;
            }, ct, access));
    }
}
