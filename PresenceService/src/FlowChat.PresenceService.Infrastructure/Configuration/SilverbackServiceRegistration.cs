using FlowChat.Core.Messaging.PresenceService.Events;
using FlowChat.PresenceService.Infrastructure.Configuration.Settings;
using FlowChat.PresenceService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.PresenceService.Infrastructure.Kafka;

public static class SilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var producerOptions = configuration.GetSection(new PresenceStatusChangedProducerSettingsSection().SectionName)
            .Get<PresenceStatusChangedProducerSettingsSection>() ?? new PresenceStatusChangedProducerSettingsSection();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(producerOptions.BootstrapServers)
                .AddProducer(producer => producer
                    .Produce<PresenceStatusChangedIntegrationEvent>("presence-status-changed", endpoint => endpoint
                        .ProduceTo(producerOptions.Topic)
                        .SerializeAsJson(serializer => serializer.SetTypeHeader())
                        .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>()))));

        return services;
    }
}
