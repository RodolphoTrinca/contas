using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record DeleteAccountCommand(string Scope, Guid Key, Guid Id, AccountAccess? Access = null) : ICommand<Result<WriteReceipt>>;
