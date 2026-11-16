using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record LinkAccountOwnerCommand(AccountId AccountId, Guid OwnerSubject, string Actor) : ICommand<Result<bool>>;
