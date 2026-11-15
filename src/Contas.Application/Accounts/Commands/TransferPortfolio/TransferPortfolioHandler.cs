using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed class TransferPortfolioHandler(IAccountAssignment assignment)
    : ICommandHandler<TransferPortfolioCommand, Result<bool>>
{
    public ValueTask<Result<bool>> Handle(TransferPortfolioCommand command, CancellationToken ct)
        => new(assignment.Transfer(command.AccountId, command.ManagerSubject, command.Actor, ct));
}
