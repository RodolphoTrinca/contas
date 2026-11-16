using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;

namespace Contas.Infrastructure.Notifications;

public sealed class SnsEventPublisher : IDisposable
{
    private readonly AmazonSimpleNotificationServiceClient client;
    private readonly string? topicArn;

    public SnsEventPublisher(IOptions<SnsOptions> options)
    {
        var settings = options.Value;
        topicArn = settings.TopicArn;
        var config = new AmazonSimpleNotificationServiceConfig
        {
            Timeout = TimeSpan.FromSeconds(4),
            MaxErrorRetry = 0
        };
        if (settings.Endpoint is { } endpoint)
        {
            config.ServiceURL = endpoint;
            config.AuthenticationRegion = settings.Region;
            client = new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials("test", "test"), config);
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);
            client = new AmazonSimpleNotificationServiceClient(config);
        }
    }

    public Task Publish(string payload, CancellationToken ct)
        => client.PublishAsync(new PublishRequest { TopicArn = topicArn, Message = payload }, ct);

    public void Dispose() => client.Dispose();
}
