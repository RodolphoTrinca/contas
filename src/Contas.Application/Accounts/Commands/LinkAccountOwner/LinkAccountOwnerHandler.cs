using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class LinkAccountOwnerHandler(IAccountAssignment assignment)
    : ICommandHandler<LinkAccountOwnerCommand, Result<bool>>
{
    public ValueTask<Result<bool>> Handle(LinkAccountOwnerCommand command, CancellationToken ct)
    {
        var subject = AccountSubject.Create(command.OwnerSubject);
        return subject.IsSuccess
            ? new(assignment.LinkOwner(command.AccountId, subject.Value, command.Actor, ct))
            : ValueTask.FromResult(Result<bool>.Failure(subject.Error));
    }
}
