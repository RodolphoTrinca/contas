using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record TransferPortfolioCommand(Guid AccountId, Guid ManagerSubject, string Actor) : ICommand<Result<bool>>;
