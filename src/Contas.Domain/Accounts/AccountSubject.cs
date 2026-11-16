namespace Contas.Domain;

public sealed record AccountSubject
{
    public Guid Value { get; }

    private AccountSubject(Guid value) => Value = value;

    public static Result<AccountSubject> Create(Guid value)
        => value == Guid.Empty
            ? Result<AccountSubject>.Failure(new("account.invalid_subject", "Informe o ID do usuário no Keycloak.", ErrorKind.Validation))
            : Result<AccountSubject>.Success(new(value));
}
