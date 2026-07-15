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

        var chatMessageSentConsumerOptions = configuration.GetSection(new ChatMessageSentConsumerSettingsSection().SectionName)
            .Get<ChatMessageSentConsumerSettingsSection>() ?? new ChatMessageSentConsumerSettingsSection();
        var presenceStatusChangedConsumerOptions = configuration.GetSection(new PresenceStatusChangedConsumerSettingsSection().SectionName)
            .Get<PresenceStatusChangedConsumerSettingsSection>() ?? new PresenceStatusChangedConsumerSettingsSection();
        var groupConversationChangedConsumerOptions = configuration.GetSection(new GroupConversationChangedConsumerSettingsSection().SectionName)
            .Get<GroupConversationChangedConsumerSettingsSection>() ?? new GroupConversationChangedConsumerSettingsSection();
        var duetConversationMembershipProjectionConsumerOptions = configuration
            .GetSection(new DuetConversationMembershipProjectionConsumerSettingsSection().SectionName)
            .Get<DuetConversationMembershipProjectionConsumerSettingsSection>()
            ?? new DuetConversationMembershipProjectionConsumerSettingsSection();

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
                        chatMessageSentConsumerOptions,
                        presenceStatusChangedConsumerOptions,
                        groupConversationChangedConsumerOptions,
                        duetConversationMembershipProjectionConsumerOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(chatMessageSentConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(chatMessageSentConsumerOptions)))
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
                        .WithGroupId(groupConversationChangedConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(groupConversationChangedConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(groupConversationChangedConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(groupConversationChangedConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(groupConversationChangedConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(groupConversationChangedConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(duetConversationMembershipProjectionConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(duetConversationMembershipProjectionConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(duetConversationMembershipProjectionConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(duetConversationMembershipProjectionConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(duetConversationMembershipProjectionConsumerOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(duetConversationMembershipProjectionConsumerOptions)))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageSentConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageSentConsumerOptions.DeadLetterTopic)
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
                            .ProduceTo(groupConversationChangedConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(groupConversationChangedConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(duetConversationMembershipProjectionConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(duetConversationMembershipProjectionConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<ChatMessageSentSubscriber>()
            .AddScopedSubscriber<UserPresenceChangedSubscriber>()
            .AddScopedSubscriber<GroupConversationChangedSubscriber>()
            .AddScopedSubscriber<GroupConversationParticipantsAddedSubscriber>()
            .AddScopedSubscriber<GroupConversationParticipantsRemovedSubscriber>()
            .AddScopedSubscriber<DuetConversationMembershipProjectionSubscriber>();

        return services;
    }

    private static string ResolveBootstrapServers(
        ChatMessageSentConsumerSettingsSection chatMessageSentConsumerOptions,
        PresenceStatusChangedConsumerSettingsSection presenceStatusChangedConsumerOptions,
        GroupConversationChangedConsumerSettingsSection groupConversationChangedConsumerOptions,
        DuetConversationMembershipProjectionConsumerSettingsSection duetConversationMembershipProjectionConsumerOptions) =>
        !string.IsNullOrWhiteSpace(chatMessageSentConsumerOptions.BootstrapServers)
            ? chatMessageSentConsumerOptions.BootstrapServers
            : !string.IsNullOrWhiteSpace(presenceStatusChangedConsumerOptions.BootstrapServers)
                ? presenceStatusChangedConsumerOptions.BootstrapServers
                : !string.IsNullOrWhiteSpace(groupConversationChangedConsumerOptions.BootstrapServers)
                    ? groupConversationChangedConsumerOptions.BootstrapServers
                    : duetConversationMembershipProjectionConsumerOptions.BootstrapServers;

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
