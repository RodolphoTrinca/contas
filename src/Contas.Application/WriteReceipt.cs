namespace Contas.Application;

public sealed record WriteReceipt(AccountView Account, bool Replayed);
