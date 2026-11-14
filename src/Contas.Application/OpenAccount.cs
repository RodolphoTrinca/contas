using Contas.Domain;

namespace Contas.Application;

public sealed class OpenAccount(IAccountStore store, TimeProvider clock)
{
    public Task<Result<WriteReceipt>> Execute(string scope, Guid key, string? name, string? cpf, CancellationToken ct)
        => store.Write(scope, key, Fingerprint.For(new { Operation = "open", name, cpf }), async accounts =>
        {
            var result = Account.Open(name, cpf, clock.GetUtcNow());
            if (!result.IsSuccess) return result;
            if (await accounts.CpfExists(result.Value.Cpf.Value, ct))
                return Result<Account>.Failure(AccountErrors.Duplicate);
            accounts.Add(result.Value);
            return result;
        }, ct);
}
