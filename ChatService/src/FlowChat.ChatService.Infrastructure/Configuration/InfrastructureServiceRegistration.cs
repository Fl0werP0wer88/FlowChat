using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Messaging.ChatService.Events;
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
            .AddProducerSettings<ChatMessageSentIntegrationEvent, ChatMessageSentProducerSettingsSection>());

        return services;
    }
}
