using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.ChatService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(InfrastructureServiceRegistration).Assembly);
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<ChatMessageSentIntegrationEvent, ChatMessageSentProducerSettingsSection>());

        return services;
    }
}
