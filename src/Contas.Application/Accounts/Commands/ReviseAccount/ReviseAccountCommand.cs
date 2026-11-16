using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record ReviseAccountCommand(string Scope, Guid Key, AccountId Id, AccountVersion ExpectedVersion, string? HolderName,
    AccountStatus? Status, AccountAccess? Access = null) : ICommand<Result<WriteReceipt>>;
