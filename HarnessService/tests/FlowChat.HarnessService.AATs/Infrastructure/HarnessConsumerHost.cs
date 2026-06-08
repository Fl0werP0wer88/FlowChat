using FlowChat.HarnessService.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class HarnessConsumerHost : IAsyncLifetime
{
    private IHost? _host;

    private readonly string _apiBaseUrl;
    private readonly string _apiKey;
    private readonly string _bootstrapServers;
    private readonly string _topic;
    private readonly string _retryTopic;
    private readonly string _deadLetterTopic;

    public HarnessConsumerHost(
        string apiBaseUrl,
        string apiKey,
        string bootstrapServers,
        string topic,
        string retryTopic,
        string deadLetterTopic)
    {
        _apiBaseUrl = apiBaseUrl;
        _apiKey = apiKey;
        _bootstrapServers = bootstrapServers;
        _topic = topic;
        _retryTopic = retryTopic;
        _deadLetterTopic = deadLetterTopic;
    }

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HarnessApi:BaseUrl"] = _apiBaseUrl,
            ["HarnessApi:ApiKey"] = _apiKey,
            ["Kafka:ProjectionConsumer:BootstrapServers"] = _bootstrapServers,
            ["Kafka:ProjectionConsumer:Topic"] = _topic,
            ["Kafka:ProjectionConsumer:RetryTopic"] = _retryTopic,
            ["Kafka:ProjectionConsumer:DeadLetterTopic"] = _deadLetterTopic,
            ["Kafka:ProjectionConsumer:GroupId"] = $"harness-aat-{Guid.NewGuid():N}",
            ["Kafka:ProjectionConsumer:RetryGroupId"] = $"harness-aat-retry-{Guid.NewGuid():N}",
            ["Kafka:ProjectionConsumer:MaxRetryCount"] = "3",
            ["Kafka:ProjectionConsumer:RetryBaseDelaySeconds"] = "1",
            ["Kafka:ProjectionConsumer:RetryMaxDelaySeconds"] = "5",
            ["Kafka:ProjectionConsumer:AutoOffsetReset"] = "Latest",
            ["Kafka:ProjectionConsumer:BatchSize"] = "100",
            ["Kafka:ProjectionConsumer:BatchMaxWaitTimeMilliseconds"] = "1000"
        });

        builder.Services.AddConsumers(builder.Configuration);

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
