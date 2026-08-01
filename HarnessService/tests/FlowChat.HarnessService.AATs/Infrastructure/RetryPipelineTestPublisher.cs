using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Kafka.Retry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class RetryPipelineTestPublisher(
    string bootstrapServers,
    string topic) : IAsyncDisposable
{
    private IHost? _host;

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, NoOpHostLifetime>());
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(bootstrapServers)
                .AddProducer(producer => producer
                    .Produce<RetryPipelineTestIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(topic)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader()))));

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task<PublishedRetryPipelineMessage> PublishAsync(
        Guid scenarioId,
        RetryPipelineTestFailureKind failureKind,
        int failuresBeforeSuccess,
        CancellationToken cancellationToken = default)
    {
        var eventId = Guid.NewGuid();
        var kafkaKey = scenarioId.ToString("D");
        var publisher = _host!.Services.GetRequiredService<IPublisher>();
        var message = new RetryPipelineTestIntegrationEvent
        {
            ScenarioId = scenarioId,
            FailureKind = failureKind,
            FailuresBeforeSuccess = failuresBeforeSuccess
        };

        await publisher.WrapAndPublishAsync(
            message,
            envelope =>
            {
                envelope.SetKafkaKey(kafkaKey);
                envelope.AddHeader(IntegrationMessageHeaders.EventId, eventId.ToString("D"));
            },
            cancellationToken);

        return new PublishedRetryPipelineMessage(eventId, kafkaKey);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is null)
            return;

        await _host.StopAsync();
        _host.Dispose();
        _host = null;
    }
}

public sealed record PublishedRetryPipelineMessage(Guid EventId, string KafkaKey);
