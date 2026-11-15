using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record LinkAccountOwnerCommand(Guid AccountId, Guid OwnerSubject, string Actor) : ICommand<Result<bool>>;
