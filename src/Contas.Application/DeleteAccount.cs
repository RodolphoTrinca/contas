using Contas.Domain;

namespace Contas.Application;

public sealed class DeleteAccount(IAccountStore store, TimeProvider clock)
{
    public Task<Result<WriteReceipt>> Execute(string scope, Guid key, Guid id, CancellationToken ct)
        => store.Write(scope, key, Fingerprint.For(new { Operation = "delete", id }), async accounts =>
        {
            var account = await accounts.Find(id, ct);
            if (account is null) return Result<Account>.Failure(AccountErrors.Missing);
            var result = account.Delete(clock.GetUtcNow());
            return result.IsSuccess ? Result<Account>.Success(account) : Result<Account>.Failure(result.Error);
        }, ct);
}
