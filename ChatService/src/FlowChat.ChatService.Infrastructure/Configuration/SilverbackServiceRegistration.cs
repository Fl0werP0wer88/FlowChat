using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public static class ApiSilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var producerOptions = configuration.GetSection(new ChatMessageSentProducerSettingsSection().SectionName)
            .Get<ChatMessageSentProducerSettingsSection>() ?? new ChatMessageSentProducerSettingsSection();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(producerOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<ChatMessageSentIntegrationEvent>("chat-message-sent", endpoint => endpoint
                            .ProduceTo(producerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}
