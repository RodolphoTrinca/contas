using Contas.Application;
using Contas.Application.Accounts.Commands;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Contas.Infrastructure.Persistence;

public sealed class DemoSeed(ISender sender, ILogger<DemoSeed> logger)
{
    public async Task<bool> Run(CancellationToken ct)
    {
        var result = await sender.Send(new OpenAccountCommand("system:seed", new Guid("23ef51ea-1d1c-4204-9cf7-a28fe41105d3"),
            "Conta Demonstração", "12345678909"), ct);
        if (result.IsSuccess || result.Error.Code == AccountErrors.Duplicate.Code) return true;
        logger.LogError("Falha no seed: {ErrorCode}", result.Error.Code);
        return false;
    }
}
