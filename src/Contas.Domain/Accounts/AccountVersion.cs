namespace Contas.Domain;

public sealed record AccountVersion
{
    public static AccountVersion Initial { get; } = new(1);

    public long Value { get; }

    private AccountVersion(long value) => Value = value;

    public static Result<AccountVersion> Create(long value)
        => value >= Initial.Value
            ? Result<AccountVersion>.Success(new(value))
            : Result<AccountVersion>.Failure(new("account.invalid_version",
                "Informe uma versão de conta positiva.", ErrorKind.Validation));

    internal Result<AccountVersion> Advance() => Value < long.MaxValue
        ? Result<AccountVersion>.Success(new(Value + 1))
        : Result<AccountVersion>.Failure(new("account.version_limit", "A conta atingiu o limite de versões.", ErrorKind.Conflict));
}
