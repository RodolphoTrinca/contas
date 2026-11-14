using Contas.Application;

namespace Contas.Infrastructure.Cache;

public sealed record CacheLookup(string? Generation, AccountView? Account);
