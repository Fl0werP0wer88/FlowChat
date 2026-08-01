using FlowChat.RealtimeService.OutboxPublisher.Configuration.Settings;
using FlowChat.RealtimeService.Persistence;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
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

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
                options.AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())
                    .WithBatchSize(runtime.BatchSize)
                    .WithInterval(runtime.PollInterval)
                    .WithExponentialRetryDelay(
                        TimeSpan.FromSeconds(runtime.RetryBaseDelaySeconds),
                        2,
                        TimeSpan.FromSeconds(runtime.MaxRetryDelaySeconds))
                    .WithoutDistributedLock());
            })
            .AddFlowChatTieredRetryProducerPipeline(kafka.BootstrapServers, kafka.Topics);

        return services;
    }
}
