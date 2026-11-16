using Contas.Domain;
using Mediator;

namespace Contas.Application.Accounts.Commands;

public sealed record TransferPortfolioCommand(AccountId AccountId, Guid ManagerSubject, string Actor) : ICommand<Result<bool>>;
