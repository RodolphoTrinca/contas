using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class DeleteAccountHandler(IAccountStore store, TimeProvider clock)
    : ICommandHandler<DeleteAccountCommand, Result<WriteReceipt>>
{
    public ValueTask<Result<WriteReceipt>> Handle(DeleteAccountCommand command, CancellationToken ct)
    {
        var (scope, key, accountId, version, access) = command;
        var id = accountId.Value;
        var expectedVersion = version.Value;
        var operation = new IdempotentWrite(scope, key, Fingerprint.For(new { Operation = "delete", id, expectedVersion }));
        return new(store.Write(operation, async accounts =>
            {
                var account = await accounts.Find(accountId, ct);
                if (account is null) return Result<Account>.Failure(AccountErrors.Missing);
                var result = account.Delete(version, clock.GetUtcNow());
                return result.IsSuccess ? Result<Account>.Success(account) : Result<Account>.Failure(result.Error);
            }, ct, access));
    }
}
