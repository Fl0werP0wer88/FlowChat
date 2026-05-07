using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.ChatService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<ISettingsProvider>(new AppSettingsProvider(configuration));
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<ChatMessageSentIntegrationEvent, ChatMessageSentProducerSettingsSection>());

        return services;
    }
}
