using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Options;

namespace Contas.PartnerDemo;

internal static class MessagingRegistration
{
    internal static void AddMessageConsumer(this IServiceCollection services)
    {
        services.AddOptions<ConsumerOptions>().BindConfiguration(ConsumerOptions.SectionName)
            .Validate(options => options.IsValid(), "Configure a fila, o consumidor e o arquivo SQLite.")
            .ValidateOnStart();
        services.AddSingleton<MessageHistory>();
        services.AddSingleton<IAmazonSQS>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ConsumerOptions>>().Value;
            var config = new AmazonSQSConfig { Timeout = TimeSpan.FromSeconds(20) };
            if (options.Endpoint is { } endpoint)
            {
                config.ServiceURL = endpoint;
                config.AuthenticationRegion = options.Region;
                return new AmazonSQSClient(new BasicAWSCredentials("test", "test"), config);
            }

            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
            return new AmazonSQSClient(config);
        });
        services.AddSingleton<QueueConsumer>();
        services.AddSingleton<IHostedService>(provider =>
            provider.GetRequiredService<IOptions<ConsumerOptions>>().Value.QueueUrl is not null
                ? provider.GetRequiredService<QueueConsumer>()
                : new DisabledQueueConsumer());
    }

    internal static async Task MapMessageHistory(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<ConsumerOptions>>().Value.QueueUrl is null) return;
        await app.Services.GetRequiredService<MessageHistory>().Initialize(CancellationToken.None);
        app.MapGet("/api/v1/messages", (long? before, MessageHistory history, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            return history.Read(before, ct);
        });
    }
}
