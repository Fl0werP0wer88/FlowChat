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

    private readonly string _connectionString;
    private readonly string _bootstrapServers;
    private readonly ProjectionKafkaAATSettings _projection;

    public string ProjectionMainGroupId { get; private set; } = string.Empty;

    public HarnessConsumerHost(
        string connectionString,
        string bootstrapServers,
        ProjectionKafkaAATSettings projection)
    {
        _connectionString = connectionString;
        _bootstrapServers = bootstrapServers;
        _projection = projection;
    }

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();

        ProjectionMainGroupId = $"{_projection.Topic}.aat";
        var projectionRetryGroupId = $"{_projection.RetryTopic}.aat";
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:HarnessDb"] = _connectionString,
            ["Kafka:ProjectionConsumer:BootstrapServers"] = _bootstrapServers,
            ["Kafka:ProjectionConsumer:Topic"] = _projection.Topic,
            ["Kafka:ProjectionConsumer:RetryTopic"] = _projection.RetryTopic,
            ["Kafka:ProjectionConsumer:DeadLetterTopic"] = _projection.DeadLetterTopic,
            ["Kafka:ProjectionConsumer:GroupId"] = ProjectionMainGroupId,
            ["Kafka:ProjectionConsumer:RetryGroupId"] = projectionRetryGroupId,
            ["Kafka:ProjectionConsumer:MaxRetryCount"] = "3",
            ["Kafka:ProjectionConsumer:RetryBaseDelaySeconds"] = "1",
            ["Kafka:ProjectionConsumer:RetryMaxDelaySeconds"] = "5",
            ["Kafka:ProjectionConsumer:AutoOffsetReset"] = "Earliest",
            ["Kafka:ProjectionConsumer:BatchSize"] = "100",
            ["Kafka:ProjectionConsumer:BatchMaxWaitTimeMilliseconds"] = "1000"
        };

        builder.Configuration.AddInMemoryCollection(settings);

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

        await _host.StartAsync();
        await KafkaConsumerGroupReadiness.WaitAsync(
            _bootstrapServers,
            [ProjectionMainGroupId, projectionRetryGroupId],
            TimeSpan.FromSeconds(60));
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
