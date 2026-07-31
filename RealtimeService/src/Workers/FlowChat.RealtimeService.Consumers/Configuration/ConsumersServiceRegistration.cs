using Confluent.Kafka;
using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Kafka.Retry;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Persistence;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.RealtimeService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(ConsumersServiceRegistration).Assembly);

        var chatMessageV2ConsumerOptions = configuration.GetSection(new ChatMessageV2ConsumerSettingsSection().SectionName)
            .Get<ChatMessageV2ConsumerSettingsSection>() ?? new ChatMessageV2ConsumerSettingsSection();
        var presenceStatusChangedConsumerOptions = configuration.GetSection(new PresenceStatusChangedConsumerSettingsSection().SectionName)
            .Get<PresenceStatusChangedConsumerSettingsSection>() ?? new PresenceStatusChangedConsumerSettingsSection();
        var conversationV2ProjectionConsumerOptions = configuration
            .GetSection(new ConversationV2ProjectionConsumerSettingsSection().SectionName)
            .Get<ConversationV2ProjectionConsumerSettingsSection>()
            ?? new ConversationV2ProjectionConsumerSettingsSection();
        var conversationMembershipV2ProjectionConsumerOptions = configuration
            .GetSection(new ConversationMembershipV2ProjectionConsumerSettingsSection().SectionName)
            .Get<ConversationMembershipV2ProjectionConsumerSettingsSection>()
            ?? new ConversationMembershipV2ProjectionConsumerSettingsSection();

        services.AddConsumerApplicationServices();
        services.AddConsumerInfrastructureServices(configuration);
        services.AddConsumerPersistenceServices(configuration);

        ITieredRetryKafkaConsumerSettingsSection[] streams =
        [
            chatMessageV2ConsumerOptions,
            presenceStatusChangedConsumerOptions,
            conversationV2ProjectionConsumerOptions,
            conversationMembershipV2ProjectionConsumerOptions
        ];
        var topology = new RealtimeRetryTopology(streams);
        services.AddSingleton(topology);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IKafkaRetryPartitionController, KafkaRetryPartitionController>();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddSingletonBrokerBehavior<DelayedRetryConsumerBehavior>()
            .AddSingletonBrokerBehavior<InvalidRetryMetadataConsumerBehavior>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkOutbox())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(
                        chatMessageV2ConsumerOptions,
                        presenceStatusChangedConsumerOptions,
                        conversationV2ProjectionConsumerOptions,
                        conversationMembershipV2ProjectionConsumerOptions));

                foreach (var stream in streams)
                {
                    clients.AddConsumer(consumer => consumer
                        .WithGroupId(stream.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(stream.AutoOffsetReset))
                        .DisableOffsetsCommit()
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureRealtimeMainEndpoint(stream)));

                    for (var tierIndex = 0; tierIndex < stream.RetryTiers.Count; tierIndex++)
                    {
                        var capturedTierIndex = tierIndex;
                        clients.AddConsumer(consumer => consumer
                            .WithGroupId(stream.RetryGroupId)
                            .WithAutoOffsetReset(ParseAutoOffsetReset(stream.AutoOffsetReset))
                            .DisableOffsetsCommit()
                            .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                            .Consume(endpoint => endpoint.ConfigureRealtimeRetryEndpoint(stream, capturedTierIndex)));
                    }

                    foreach (var destinationTopic in stream.RetryTiers.Select(tier => tier.Topic).Append(stream.DeadLetterTopic))
                    {
                        clients.AddProducer(producer => producer
                            .Produce(destinationTopic, endpoint => endpoint
                                .ProduceTo(destinationTopic)
                                .SerializeAsJson(serializer => serializer.SetTypeHeader())
                                .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
                    }
                }
            })
            .AddScopedSubscriber<ChatMessageSentV2Subscriber>()
            .AddScopedSubscriber<UserPresenceChangedSubscriber>()
            .AddScopedSubscriber<ConversationProjectionV2Subscriber>()
            .AddScopedSubscriber<ConversationMembershipDeltaV2Subscriber>();

        return services;
    }

    private static string ResolveBootstrapServers(
        ChatMessageV2ConsumerSettingsSection chatMessageV2ConsumerOptions,
        PresenceStatusChangedConsumerSettingsSection presenceStatusChangedConsumerOptions,
        ConversationV2ProjectionConsumerSettingsSection conversationV2ProjectionConsumerOptions,
        ConversationMembershipV2ProjectionConsumerSettingsSection conversationMembershipV2ProjectionConsumerOptions) =>
        !string.IsNullOrWhiteSpace(chatMessageV2ConsumerOptions.BootstrapServers)
            ? chatMessageV2ConsumerOptions.BootstrapServers
            : !string.IsNullOrWhiteSpace(presenceStatusChangedConsumerOptions.BootstrapServers)
                ? presenceStatusChangedConsumerOptions.BootstrapServers
                : !string.IsNullOrWhiteSpace(conversationV2ProjectionConsumerOptions.BootstrapServers)
                    ? conversationV2ProjectionConsumerOptions.BootstrapServers
                    : conversationMembershipV2ProjectionConsumerOptions.BootstrapServers;

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
