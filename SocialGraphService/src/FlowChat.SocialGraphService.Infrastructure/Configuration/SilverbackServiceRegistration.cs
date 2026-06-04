using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
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
        var contactProjectionOptions = configuration.GetSection(new ContactProjectionProducerSettingsSection().SectionName)
            .Get<ContactProjectionProducerSettingsSection>() ?? new ContactProjectionProducerSettingsSection();

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
                    .WithBootstrapServers(contactProjectionOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<ContactReadModel>>("social-graph-contact-projection", endpoint => endpoint
                            .ProduceTo(contactProjectionOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}
