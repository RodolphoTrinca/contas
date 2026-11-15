using System.Diagnostics.CodeAnalysis;

namespace Contas.Domain;

public sealed class Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    [MemberNotNullWhen(false, nameof(Error))]
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess => Error is null;

    private Result(T value) => Value = value;
    private Result(Error error) => Error = error;

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);
}
