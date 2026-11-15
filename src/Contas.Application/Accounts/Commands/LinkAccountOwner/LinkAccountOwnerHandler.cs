using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class LinkAccountOwnerHandler(IAccountAssignment assignment)
    : ICommandHandler<LinkAccountOwnerCommand, Result<bool>>
{
    public ValueTask<Result<bool>> Handle(LinkAccountOwnerCommand command, CancellationToken ct)
        => new(assignment.LinkOwner(command.AccountId, command.OwnerSubject, command.Actor, ct));
}
