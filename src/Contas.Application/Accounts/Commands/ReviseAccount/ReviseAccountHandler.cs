using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class ReviseAccountHandler(IAccountStore store, TimeProvider clock)
    : ICommandHandler<ReviseAccountCommand, Result<WriteReceipt>>
{
    public ValueTask<Result<WriteReceipt>> Handle(ReviseAccountCommand command, CancellationToken ct)
    {
        var (scope, key, id, name, status, access) = command;
        return new(store.Write(scope, key, Fingerprint.For(new { Operation = "revise", id, name, status }), async accounts =>
            {
                var account = await accounts.Find(id, ct);
                if (account is null) return Result<Account>.Failure(AccountErrors.Missing);
                var result = account.Revise(name, status, clock.GetUtcNow());
                return result.IsSuccess ? Result<Account>.Success(account) : Result<Account>.Failure(result.Error);
            }, ct, access));
    }
}
