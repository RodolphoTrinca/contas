namespace Contas.Domain;

public sealed class Account
{
    public AccountId Id { get; }
    public AccountHolderName HolderName { get; private set; }
    public Cpf Cpf { get; private set; }
    public AccountStatus Status { get; private set; }
    public AccountVersion Version { get; private set; } = AccountVersion.Initial;
    public AccountSubject? Manager { get; private set; }
    public AccountSubject? Owner { get; private set; }
    public bool Deleted { get; private set; }
    public AccountEvent? PendingEvent { get; private set; }

    private Account(AccountId id, AccountHolderName holderName, Cpf cpf)
        => (Id, HolderName, Cpf, Status) = (id, holderName, cpf, AccountStatus.Active);

    public static Result<Account> Open(string? name, string? cpf, DateTimeOffset now, AccountSubject? manager = null)
    {
        var validatedName = AccountHolderName.Create(name);
        if (!validatedName.IsSuccess) return Result<Account>.Failure(validatedName.Error);
        var validatedCpf = Cpf.Create(cpf);
        if (!validatedCpf.IsSuccess) return Result<Account>.Failure(validatedCpf.Error);
        var account = new Account(AccountId.New(), validatedName.Value, validatedCpf.Value) { Manager = manager };
        account.Record(AccountEventKind.AccountCreated, now);
        return Result<Account>.Success(account);
    }

    public static Account Restore(AccountId id, AccountHolderName name, Cpf cpf, AccountStatus status, AccountVersion version, bool deleted,
        AccountSubject? manager, AccountSubject? owner)
        => new(id, name, cpf) { Status = status, Version = version, Deleted = deleted, Manager = manager, Owner = owner };

    public Result<bool> Revise(string? name, AccountStatus? status, AccountVersion expectedVersion, DateTimeOffset now)
    {
        if (Deleted) return AlreadyDeleted();
        if (Version != expectedVersion) return StaleVersion();
        var validatedName = AccountHolderName.Create(name);
        if (!validatedName.IsSuccess) return Result<bool>.Failure(validatedName.Error);
        if (status is not AccountStatus next || !Enum.IsDefined(next))
            return Result<bool>.Failure(new("account.invalid_status", "Informe active ou inactive.", ErrorKind.Validation));
        if (HolderName == validatedName.Value && Status == next)
            return Result<bool>.Success(false);
        var nextVersion = Version.Advance();
        if (!nextVersion.IsSuccess) return Result<bool>.Failure(nextVersion.Error);
        HolderName = validatedName.Value;
        Status = next;
        Version = nextVersion.Value;
        Record(AccountEventKind.AccountUpdated, now);
        return Result<bool>.Success(true);
    }

    public Result<bool> Delete(AccountVersion expectedVersion, DateTimeOffset now)
    {
        if (Deleted) return AlreadyDeleted();
        if (Version != expectedVersion) return StaleVersion();
        var nextVersion = Version.Advance();
        if (!nextVersion.IsSuccess) return Result<bool>.Failure(nextVersion.Error);
        Deleted = true;
        Version = nextVersion.Value;
        Record(AccountEventKind.AccountDeleted, now);
        return Result<bool>.Success(true);
    }

    private static Result<bool> StaleVersion() =>
        Result<bool>.Failure(new("account.stale_version", "A conta mudou. Atualize a lista antes de tentar novamente.", ErrorKind.Conflict));

    private static Result<bool> AlreadyDeleted() =>
        Result<bool>.Failure(new("account.deleted", "A conta foi excluída.", ErrorKind.Conflict));

    private void Record(AccountEventKind type, DateTimeOffset now)
        => PendingEvent = new(Guid.NewGuid(), Id, type, Version, Status, now);

    public Result<bool> TransferPortfolio(AccountSubject manager)
    {
        if (Deleted) return AlreadyDeleted();
        if (Manager == manager) return Result<bool>.Success(false);
        Manager = manager;
        return Result<bool>.Success(true);
    }

    public Result<bool> LinkOwner(AccountSubject owner)
    {
        if (Deleted) return AlreadyDeleted();
        if (Owner == owner) return Result<bool>.Success(false);
        Owner = owner;
        return Result<bool>.Success(true);
    }
}
