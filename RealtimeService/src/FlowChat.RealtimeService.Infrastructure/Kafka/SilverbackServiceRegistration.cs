using FlowChat.Core.Messaging.RealtimeService.Events;
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
        var settingsManager = new KafkaSettingsManager(configuration);
        var producerOptions = settingsManager.GetRealtimeConnectionProducerOptions();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(producerOptions.BootstrapServers)
                .AddProducer(producer => producer
                    .Produce<RealtimeConnectionRegisteredIntegrationEvent>("realtime-connection-registered", endpoint => endpoint
                        .ProduceTo(producerOptions.Topic)
                        .SetKafkaKey(message => message?.Key)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                .AddProducer(producer => producer
                    .Produce<RealtimeConnectionUnregisteredIntegrationEvent>("realtime-connection-unregistered", endpoint => endpoint
                        .ProduceTo(producerOptions.Topic)
                        .SetKafkaKey(message => message?.Key)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader()))));

        return services;
    }
}
