using Contas.Domain;

namespace Contas.Application;

public sealed class ReviseAccount(IAccountStore store, TimeProvider clock)
{
    public Task<Result<WriteReceipt>> Execute(string scope, Guid key, Guid id, string? name, AccountStatus? status, CancellationToken ct)
        => store.Write(scope, key, Fingerprint.For(new { Operation = "revise", id, name, status }), async accounts =>
        {
            var account = await accounts.Find(id, ct);
            if (account is null) return Result<Account>.Failure(AccountErrors.Missing);
            var result = account.Revise(name, status, clock.GetUtcNow());
            return result.IsSuccess ? Result<Account>.Success(account) : Result<Account>.Failure(result.Error);
        }, ct);
}
