using FlowChat.RealtimeService.Application;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Infrastructure;
using FlowChat.RealtimeService.Persistence;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
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

        ITieredRetryKafkaConsumerSettingsSection[] streams =
        [
            chatMessageV2ConsumerOptions,
            presenceStatusChangedConsumerOptions,
            conversationV2ProjectionConsumerOptions,
            conversationMembershipV2ProjectionConsumerOptions
        ];
        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddFlowChatTieredRetryConsumerPipeline<AppDbContext>(
                ResolveBootstrapServers(
                    chatMessageV2ConsumerOptions,
                    presenceStatusChangedConsumerOptions,
                    conversationV2ProjectionConsumerOptions,
                    conversationMembershipV2ProjectionConsumerOptions),
                streams)
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkOutbox())
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

}
