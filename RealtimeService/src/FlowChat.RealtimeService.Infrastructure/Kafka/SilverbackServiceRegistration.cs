using FlowChat.Core.Messaging.RealtimeService.Events;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.RealtimeService.Infrastructure.Kafka;

public static class SilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsProvider = new AppSettingsProvider(configuration);
        var registeredProducerOptions = settingsProvider.GetSection<RealtimeConnectionRegisteredProducerSettingsSection>();
        var unregisteredProducerOptions = settingsProvider.GetSection<RealtimeConnectionUnregisteredProducerSettingsSection>();
        var bootstrapServers = !string.IsNullOrWhiteSpace(registeredProducerOptions.BootstrapServers)
            ? registeredProducerOptions.BootstrapServers
            : unregisteredProducerOptions.BootstrapServers;

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(bootstrapServers)
                .AddProducer(producer => producer
                    .Produce<RealtimeConnectionRegisteredIntegrationEvent>("realtime-connection-registered", endpoint => endpoint
                        .ProduceTo(registeredProducerOptions.Topic)
                        .SetKafkaKey(message => message?.Key)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                .AddProducer(producer => producer
                    .Produce<RealtimeConnectionUnregisteredIntegrationEvent>("realtime-connection-unregistered", endpoint => endpoint
                        .ProduceTo(unregisteredProducerOptions.Topic)
                        .SetKafkaKey(message => message?.Key)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader()))));

        return services;
    }
}
