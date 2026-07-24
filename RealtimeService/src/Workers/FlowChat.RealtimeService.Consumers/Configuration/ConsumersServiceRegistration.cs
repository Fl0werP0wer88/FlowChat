using Confluent.Kafka;
using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Persistence;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

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

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(
                        chatMessageV2ConsumerOptions,
                        presenceStatusChangedConsumerOptions,
                        conversationV2ProjectionConsumerOptions,
                        conversationMembershipV2ProjectionConsumerOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageV2ConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageV2ConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(chatMessageV2ConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageV2ConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageV2ConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(chatMessageV2ConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(presenceStatusChangedConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(presenceStatusChangedConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(presenceStatusChangedConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(presenceStatusChangedConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(presenceStatusChangedConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(presenceStatusChangedConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(conversationV2ProjectionConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(conversationV2ProjectionConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(conversationV2ProjectionConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(conversationV2ProjectionConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(conversationV2ProjectionConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(conversationV2ProjectionConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(conversationMembershipV2ProjectionConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(conversationMembershipV2ProjectionConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(conversationMembershipV2ProjectionConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(conversationMembershipV2ProjectionConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(conversationMembershipV2ProjectionConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(conversationMembershipV2ProjectionConsumerOptions)))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageV2ConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageV2ConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(presenceStatusChangedConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(presenceStatusChangedConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(conversationV2ProjectionConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(conversationV2ProjectionConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(conversationMembershipV2ProjectionConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(conversationMembershipV2ProjectionConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
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
