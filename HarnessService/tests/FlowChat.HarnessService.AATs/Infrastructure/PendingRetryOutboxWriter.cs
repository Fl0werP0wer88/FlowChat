using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Kafka.Retry;
using FlowChat.HarnessService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class PendingRetryOutboxWriter(
    string connectionString,
    string bootstrapServers,
    string retryTopic) : IAsyncDisposable
{
    private IHost? _host;

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, NoOpHostLifetime>());
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<SilverbackEfUnitOfWork<AppDbContext>>();
        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(bootstrapServers)
                .AddProducer(producer => producer
                    .Produce<RetryPipelineTestIntegrationEvent>(retryTopic, endpoint => endpoint
                        .ProduceTo(retryTopic)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader())
                        .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>()))));

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async Task WriteAsync(
        Guid scenarioId,
        string sourceTopic,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _host!.Services.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<SilverbackEfUnitOfWork<AppDbContext>>();
        var now = DateTimeOffset.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                dbContext.SilverbackStoredOffsets.Add(new SilverbackStoredOffset
                {
                    GroupId = $"harness-restarted-{scenarioId:N}",
                    Topic = sourceTopic,
                    Partition = 0,
                    Offset = 0
                });

                await publisher.WrapAndPublishAsync(
                    new RetryPipelineTestIntegrationEvent
                    {
                        ScenarioId = scenarioId,
                        FailureKind = RetryPipelineTestFailureKind.Transient,
                        FailuresBeforeSuccess = 0
                    },
                    envelope =>
                    {
                        envelope.SetKafkaKey(scenarioId.ToString("D"));
                        envelope.AddHeader(IntegrationMessageHeaders.EventId, Guid.NewGuid().ToString("D"));
                        envelope.AddHeader(RetryMessageHeaders.RetryAttempt, "1");
                        envelope.AddHeader(RetryMessageHeaders.RetryAtUtc, now.ToString("O"));
                        envelope.AddHeader(RetryMessageHeaders.FirstFailedAtUtc, now.ToString("O"));
                        envelope.AddHeader(RetryMessageHeaders.OriginalTopic, sourceTopic);
                        envelope.AddHeader(RetryMessageHeaders.OriginalPartition, "0");
                        envelope.AddHeader(RetryMessageHeaders.OriginalOffset, "0");
                        envelope.AddHeader(RetryMessageHeaders.LastErrorType, typeof(Exception).FullName!);
                    },
                    transactionCancellationToken);

                await unitOfWork.SaveChangesAsync(transactionCancellationToken);
                return true;
            },
            cancellationToken);
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
