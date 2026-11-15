using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record OpenAccountCommand(string Scope, Guid Key, string? HolderName, string? Cpf,
    AccountAccess? Access = null) : ICommand<Result<WriteReceipt>>;
