using FlowChat.Core.Messaging.SocialGraphService.Events;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.SocialGraphService.Persistence;
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
        var contactAddedOptions = configuration.GetSection(new ContactAddedProducerSettingsSection().SectionName)
            .Get<ContactAddedProducerSettingsSection>() ?? new ContactAddedProducerSettingsSection();
        var contactDeletedOptions = configuration.GetSection(new ContactDeletedProducerSettingsSection().SectionName)
            .Get<ContactDeletedProducerSettingsSection>() ?? new ContactDeletedProducerSettingsSection();
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
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<ContactDeletedIntegrationEvent>("social-graph-contact-deleted", endpoint => endpoint
                            .ProduceTo(contactDeletedOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}
