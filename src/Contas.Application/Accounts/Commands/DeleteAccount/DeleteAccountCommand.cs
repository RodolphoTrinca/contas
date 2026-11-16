using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record DeleteAccountCommand(string Scope, Guid Key, AccountId Id, AccountVersion ExpectedVersion, AccountAccess? Access = null) : ICommand<Result<WriteReceipt>>;
