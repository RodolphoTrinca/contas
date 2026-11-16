namespace Contas.PartnerDemo;

internal sealed class DisabledQueueConsumer : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
