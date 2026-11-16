namespace Contas.Domain;

public sealed record AccountHolderName
{
    public string Value { get; }

    private AccountHolderName(string value) => Value = value;

    public static Result<AccountHolderName> Create(string? input)
    {
        var value = input?.Trim() ?? "";
        return value.Length is >= 2 and <= 120
            ? Result<AccountHolderName>.Success(new(value))
            : Result<AccountHolderName>.Failure(new("account.invalid_name", "O nome deve ter entre 2 e 120 caracteres.", ErrorKind.Validation));
    }
}
