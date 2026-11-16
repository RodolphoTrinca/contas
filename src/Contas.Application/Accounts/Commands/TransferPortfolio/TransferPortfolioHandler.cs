using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class TransferPortfolioHandler(IAccountAssignment assignment)
    : ICommandHandler<TransferPortfolioCommand, Result<bool>>
{
    public ValueTask<Result<bool>> Handle(TransferPortfolioCommand command, CancellationToken ct)
    {
        var subject = AccountSubject.Create(command.ManagerSubject);
        return subject.IsSuccess
            ? new(assignment.Transfer(command.AccountId, subject.Value, command.Actor, ct))
            : ValueTask.FromResult(Result<bool>.Failure(subject.Error));
    }
}
