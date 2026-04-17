using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.ChatService.Infrastructure.Kafka;
using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.ChatService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<ISettingsProvider>(new SettingsProvider(configuration));
        services.AddScoped<IKafkaProducerSettingsSection<ChatMessageSentIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<ChatMessageSentProducerSettingsSection>());
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();

        return services;
    }
}
