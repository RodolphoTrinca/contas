using System.Text.RegularExpressions;

namespace Contas.Domain;

public sealed record Cpf
{
    public string Value { get; }
    private Cpf(string value) => Value = value;

    public static Result<Cpf> Create(string? input)
    {
        var value = input?.Trim() ?? "";
        if (!Regex.IsMatch(value, @"\A(?:[0-9]{11}|[0-9]{3}\.[0-9]{3}\.[0-9]{3}-[0-9]{2})\z"))
            return Invalid();
        value = value.Replace(".", "").Replace("-", "");
        return value.All(digit => digit == value[0]) ||
               CheckDigit(value, 9) != value[9] || CheckDigit(value, 10) != value[10]
            ? Invalid()
            : Result<Cpf>.Success(new Cpf(value));
    }

    private static Result<Cpf> Invalid() =>
        Result<Cpf>.Failure(new("account.invalid_cpf", "Informe um CPF válido.", ErrorKind.Validation));

    private static char CheckDigit(string value, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += (value[i] - '0') * (length + 1 - i);
        var remainder = sum % 11;
        return (char)('0' + (remainder < 2 ? 0 : 11 - remainder));
    }
}
