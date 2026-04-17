using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.ChatService.Infrastructure.Configuration;
using FlowChat.ChatService.Infrastructure.Kafka;
using FlowChat.Core.Messaging.ChatService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.ChatService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IWorkerSettingsManager>(new WorkerSettingsManager(configuration));
        services.AddScoped<IKafkaProducerOptions<ChatMessageSentIntegrationEvent>>(sp =>
            sp.GetRequiredService<IWorkerSettingsManager>().GetChatMessageSentProducerSettingsSection());
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();

        return services;
    }
}

