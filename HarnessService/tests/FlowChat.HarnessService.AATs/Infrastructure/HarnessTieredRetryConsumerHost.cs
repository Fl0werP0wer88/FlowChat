using FlowChat.HarnessService.Consumers;
using FlowChat.HarnessService.Application.Features.KafkaRetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class HarnessTieredRetryConsumerHost(
    string connectionString,
    string bootstrapServers,
    RetryPipelineKafkaAATSettings retryPipeline) : IAsyncLifetime
{
    private IHost? _host;

    public string MainGroupId { get; private set; } = string.Empty;
    public TestLogCollector LogCollector { get; } = new();

    public async Task InitializeAsync()
    {
        MainGroupId = $"{retryPipeline.Topic}.aat";
        var retryGroupId = $"{retryPipeline.Topic}.retry.aat";
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:HarnessDb"] = connectionString,
            ["Kafka:RetryPipelineConsumer:BootstrapServers"] = bootstrapServers,
            ["Kafka:RetryPipelineConsumer:GroupId"] = MainGroupId,
            ["Kafka:RetryPipelineConsumer:RetryGroupId"] = retryGroupId,
            ["Kafka:RetryPipelineConsumer:Topic"] = retryPipeline.Topic,
            ["Kafka:RetryPipelineConsumer:DeadLetterTopic"] = retryPipeline.DeadLetterTopic,
            ["Kafka:RetryPipelineConsumer:AutoOffsetReset"] = "Earliest"
        };

        for (var index = 0; index < retryPipeline.RetryTiers.Count; index++)
        {
            settings[$"Kafka:RetryPipelineConsumer:RetryTiers:{index}:Topic"] = retryPipeline.RetryTiers[index].Topic;
            settings[$"Kafka:RetryPipelineConsumer:RetryTiers:{index}:Delay"] = retryPipeline.RetryTiers[index].Delay.ToString("c");
        }

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddTieredRetryHarnessConsumers(builder.Configuration);
        builder.Services.Configure<HostOptions>(options =>
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, NoOpHostLifetime>());
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddProvider(LogCollector);

        _host = builder.Build();
        await _host.StartAsync();
        await KafkaConsumerGroupReadiness.WaitAsync(
            bootstrapServers,
            [MainGroupId, retryGroupId],
            TimeSpan.FromSeconds(60));
    }

    public int GetAttemptCount(Guid scenarioId) =>
        _host?.Services
            .GetRequiredService<RetryPipelineTestAttemptTracker>()
            .GetAttemptCount(scenarioId)
        ?? 0;

    public async Task DisposeAsync()
    {
        if (_host is null)
            return;

        await _host.StopAsync();
        _host.Dispose();
        _host = null;
    }

}
