using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.ChatService.Infrastructure;

public static class ApiInfrastructureServiceRegistration
{
    public static IServiceCollection AddApiInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

public static class ConsumerInfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

internal static class CommonInfrastructureServiceRegistration
{
    public static IServiceCollection AddCommonInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(CommonInfrastructureServiceRegistration).Assembly);
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<ChatMessageSentIntegrationEvent, ChatMessageSentProducerSettingsSection>()
            .AddProducerSettings<GroupConversationChangedIntegrationEvent, GroupConversationChangedProducerSettingsSection>()
            .AddProducerSettings<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>, GroupConversationProjectionProducerSettingsSection>()
            .AddProducerSettings<ProjectionIntegrationEvent<DuetConversationMembershipReadModel>, DuetConversationProjectionProducerSettingsSection>()
            .AddProducerSettings<ProjectionIntegrationEvent<DuetConversationContactStateReadModel>, DuetConversationProjectionProducerSettingsSection>()
            .AddProducerSettings<ProjectionIntegrationEvent<ConversationReadModelV2>, ConversationV2ProducerSettingsSection>()
            .AddProducerSettings<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>, ConversationMembershipV2ProjectionProducerSettingsSection>()
            .AddProducerSettings<ProjectionIntegrationEvent<ConversationParticipantReadModelV2>, ConversationParticipantV2ProducerSettingsSection>()
            .AddProducerSettings<ChatMessageSentIntegrationEventV2, ChatMessageV2ProducerSettingsSection>());

        return services;
    }
}
