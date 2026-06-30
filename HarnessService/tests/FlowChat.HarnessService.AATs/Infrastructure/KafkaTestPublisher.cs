using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Projections.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

namespace FlowChat.HarnessService.AATs.Infrastructure;

/// <summary>
/// Publishes test messages to Kafka via a minimal Silverback producer host so that messages carry
/// the x-message-type header that the consumer's WithOptionalMessageTypeHeader deserializer needs
/// to route them to the correct subscriber.
/// </summary>
public sealed class KafkaTestPublisher : IAsyncDisposable
{
    private IHost? _host;
    private readonly string _bootstrapServers;
    private readonly string _topic;

    public KafkaTestPublisher(string bootstrapServers, string topic)
    {
        _bootstrapServers = bootstrapServers;
        _topic = topic;
    }

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, NoOpHostLifetime>());
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
                clients.WithBootstrapServers(_bootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<ProjectionTestReadModel>>(endpoint => endpoint
                            .ProduceTo(_topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader()))));

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task PublishAsync(
        Guid id,
        string payload,
        int version,
        OperationType operation = OperationType.Created,
        CancellationToken cancellationToken = default)
    {
        var publisher = _host!.Services.GetRequiredService<IPublisher>();
        var now = DateTimeOffset.UtcNow;
        await publisher.PublishAsync(new ProjectionIntegrationEvent<ProjectionTestReadModel>
        {
            SourceAggregateId = id,
            SourceAggregateCreatedAtUtc = now,
            SourceAggregateModifiedAtUtc = now,
            SourceAggregateDeletedAt = operation == OperationType.Deleted ? now : null,
            Value = new ProjectionTestReadModel { Payload = payload },
            Operation = operation,
            SourceAggregateVersion = version
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
