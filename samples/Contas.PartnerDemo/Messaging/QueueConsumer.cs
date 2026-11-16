using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Contas.PartnerDemo;

internal sealed class QueueConsumer(IAmazonSQS sqs, IOptions<ConsumerOptions> options, MessageHistory history,
    ILogger<QueueConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var queue = options.Value.QueueUrl;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queue,
                    WaitTimeSeconds = 10,
                    MaxNumberOfMessages = 10
                }, ct);
                foreach (var message in response.Messages ?? [])
                    await Process(queue!, message, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception error) when (!ct.IsCancellationRequested &&
                error is AmazonServiceException or AmazonClientException or HttpRequestException or OperationCanceledException)
            {
                logger.LogWarning("Falha ao consumir fila: {ErrorType}", error.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    private async Task Process(string queue, Message delivery, CancellationToken ct)
    {
        try
        {
            var message = JsonSerializer.Deserialize<AccountNotification>(delivery.Body);
            if (message is null || !message.IsValid())
            {
                logger.LogWarning("Evento da fila inválido, aguardando política de redrive");
                return;
            }

            await history.RecordReceipt(message, delivery.Body, ct);
            await history.RecordProcessing(message, ct);
            await sqs.DeleteMessageAsync(queue, delivery.ReceiptHandle, ct);
        }
        catch (Exception error) when (!ct.IsCancellationRequested && error is JsonException or SqliteException)
        {
            logger.LogWarning("Mensagem permanece na fila: {ErrorType}", error.GetType().Name);
        }
    }
}
