using FlowChat.RealtimeService.OutboxPublisher.Configuration.Settings;
using FlowChat.RealtimeService.Persistence;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.RealtimeService.OutboxPublisher;

public static class OutboxPublisherServiceRegistration
{
    public static IServiceCollection AddOutboxPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(OutboxPublisherServiceRegistration).Assembly);

        var runtime = configuration.GetSection(new OutboxPublisherRuntimeSettingsSection().SectionName)
            .Get<OutboxPublisherRuntimeSettingsSection>() ?? new OutboxPublisherRuntimeSettingsSection();
        var kafka = configuration.GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>() ?? new RetryOutboxKafkaSettingsSection();

        if (kafka.Topics.Count == 0 || kafka.Topics.Any(string.IsNullOrWhiteSpace) || kafka.Topics.Distinct().Count() != kafka.Topics.Count)
            throw new InvalidOperationException("Retry outbox publisher topics must be non-empty and unique.");

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
                options.AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())
                    .WithBatchSize(runtime.BatchSize)
                    .WithInterval(TimeSpan.FromSeconds(runtime.PollIntervalSeconds))
                    .WithExponentialRetryDelay(
                        TimeSpan.FromSeconds(runtime.RetryBaseDelaySeconds),
                        2,
                        TimeSpan.FromSeconds(runtime.MaxRetryDelaySeconds))
                    .WithoutDistributedLock());
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(kafka.BootstrapServers);
                foreach (var topic in kafka.Topics)
                {
                    clients.AddProducer(producer => producer
                        .Produce(topic, endpoint => endpoint
                            .ProduceTo(topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
                }
            });

        return services;
    }
}
