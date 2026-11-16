namespace Contas.Domain;

public sealed class Account
{
    public Guid Id { get; private set; }
    public string HolderName { get; private set; }
    public Cpf Cpf { get; private set; }
    public AccountStatus Status { get; private set; }
    public long Version { get; private set; }
    public bool Deleted { get; private set; }
    public AccountEvent? PendingEvent { get; private set; }

    private Account(Guid id, string holderName, Cpf cpf)
        => (Id, HolderName, Cpf, Status) = (id, holderName, cpf, AccountStatus.Active);

    public static Result<Account> Open(string? name, string? cpf, DateTimeOffset now)
    {
        var validatedName = ValidateName(name);
        if (!validatedName.IsSuccess) return Result<Account>.Failure(validatedName.Error);
        var validatedCpf = Cpf.Create(cpf);
        if (!validatedCpf.IsSuccess) return Result<Account>.Failure(validatedCpf.Error);
        var account = new Account(Guid.NewGuid(), validatedName.Value, validatedCpf.Value);
        account.Record("AccountCreated", now);
        return Result<Account>.Success(account);
    }

    public static Account Restore(Guid id, string name, Cpf cpf, AccountStatus status, long version, bool deleted)
        => new(id, name, cpf) { Status = status, Version = version, Deleted = deleted };

    public Result<bool> Revise(string? name, AccountStatus? status, long expectedVersion, DateTimeOffset now)
    {
        if (Deleted) return AlreadyDeleted();
        if (Version != expectedVersion) return StaleVersion();
        var validatedName = ValidateName(name);
        if (!validatedName.IsSuccess) return Result<bool>.Failure(validatedName.Error);
        if (status is not AccountStatus next || !Enum.IsDefined(next))
            return Result<bool>.Failure(new("account.invalid_status", "Informe active ou inactive.", ErrorKind.Validation));
        if (HolderName == validatedName.Value && Status == next)
            return Result<bool>.Success(false);
        HolderName = validatedName.Value;
        Status = next;
        Record("AccountUpdated", now);
        return Result<bool>.Success(true);
    }

    public Result<bool> Delete(long expectedVersion, DateTimeOffset now)
    {
        if (Deleted) return AlreadyDeleted();
        if (Version != expectedVersion) return StaleVersion();
        Deleted = true;
        Record("AccountDeleted", now);
        return Result<bool>.Success(true);
    }

    private static Result<bool> StaleVersion() =>
        Result<bool>.Failure(new("account.stale_version", "A conta mudou. Atualize a lista antes de tentar novamente.", ErrorKind.Conflict));

    private static Result<bool> AlreadyDeleted() =>
        Result<bool>.Failure(new("account.deleted", "A conta foi excluída.", ErrorKind.Conflict));

    private void Record(string type, DateTimeOffset now)
        => PendingEvent = new(Guid.NewGuid(), Id, type, ++Version, Status, now);

    private static Result<string> ValidateName(string? name)
    {
        var value = name?.Trim() ?? "";
        return value.Length is >= 2 and <= 120
            ? Result<string>.Success(value)
            : Result<string>.Failure(new("account.invalid_name", "O nome deve ter entre 2 e 120 caracteres.", ErrorKind.Validation));
    }
}
