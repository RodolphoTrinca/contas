using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class ReviseAccountHandler(IAccountStore store, TimeProvider clock)
    : ICommandHandler<ReviseAccountCommand, Result<WriteReceipt>>
{
    public ValueTask<Result<WriteReceipt>> Handle(ReviseAccountCommand command, CancellationToken ct)
    {
        var (scope, key, accountId, version, name, status, access) = command;
        var id = accountId.Value;
        var expectedVersion = version.Value;
        var operation = new IdempotentWrite(scope, key, Fingerprint.For(new { Operation = "revise", id, name, status, expectedVersion }));
        return new(store.Write(operation, async accounts =>
            {
                var account = await accounts.Find(accountId, ct);
                if (account is null) return Result<Account>.Failure(AccountErrors.Missing);
                var result = account.Revise(name, status, version, clock.GetUtcNow());
                return result.IsSuccess ? Result<Account>.Success(account) : Result<Account>.Failure(result.Error);
            }, ct, access));
    }
}
