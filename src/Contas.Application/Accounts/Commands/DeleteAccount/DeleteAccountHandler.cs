using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class DeleteAccountHandler(IAccountStore store, TimeProvider clock)
    : ICommandHandler<DeleteAccountCommand, Result<WriteReceipt>>
{
    public ValueTask<Result<WriteReceipt>> Handle(DeleteAccountCommand command, CancellationToken ct)
    {
        var (scope, key, id, expectedVersion, access) = command;
        return new(store.Write(scope, key, Fingerprint.For(new { Operation = "delete", id, expectedVersion }), async accounts =>
            {
                var account = await accounts.Find(id, ct);
                if (account is null) return Result<Account>.Failure(AccountErrors.Missing);
                var result = account.Delete(expectedVersion, clock.GetUtcNow());
                return result.IsSuccess ? Result<Account>.Success(account) : Result<Account>.Failure(result.Error);
            }, ct, access));
    }
}
