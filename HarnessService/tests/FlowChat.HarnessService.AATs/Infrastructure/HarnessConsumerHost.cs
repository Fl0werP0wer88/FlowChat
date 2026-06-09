using FlowChat.HarnessService.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

        builder.Services.Configure<HostOptions>(opts =>
            opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

        // Replace ConsoleLifetime (which reacts to Ctrl+C / SIGTERM) with a no-op lifetime so the
        // test process's signal handling cannot inadvertently stop the in-process consumer host.
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, NoOpHostLifetime>());

        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddFilter("FlowChat.HarnessService", LogLevel.Debug);
        builder.Logging.AddFilter("Silverback.Messaging.Consuming", LogLevel.Debug);

        _host = builder.Build();

        var lifetime = _host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() =>
            Console.Error.WriteLine($"\n=== CONSUMER HOST STOPPING — caller stack:\n{Environment.StackTrace}\n===\n"));

        await _host.StartAsync();

        // Allow time for Kafka partition assignment to complete so that messages published immediately
        // after InitializeAsync returns are not missed due to AutoOffsetReset=Latest race condition.
        await Task.Delay(TimeSpan.FromSeconds(5));
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
