using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.SocialGraphService.Infrastructure.Kafka;

public static class SilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsManager = new KafkaSettingsManager(configuration);
        var contactAddedOptions = settingsManager.GetContactAddedProducerOptions();
        var contactDeletedOptions = settingsManager.GetContactDeletedProducerOptions();
        var bootstrapServers = !string.IsNullOrWhiteSpace(contactAddedOptions.BootstrapServers)
            ? contactAddedOptions.BootstrapServers
            : contactDeletedOptions.BootstrapServers;

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(bootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<ContactAddedIntegrationEvent>("social-graph-contact-added", endpoint => endpoint
                            .ProduceTo(contactAddedOptions.Topic)
                            .SetKafkaKey(message => message?.Key)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<ContactDeletedIntegrationEvent>("social-graph-contact-deleted", endpoint => endpoint
                            .ProduceTo(contactDeletedOptions.Topic)
                            .SetKafkaKey(message => message?.Key)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}
