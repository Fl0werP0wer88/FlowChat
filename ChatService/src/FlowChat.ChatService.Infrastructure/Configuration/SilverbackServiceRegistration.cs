using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public static class SilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<ISettingsProvider>(new AppSettingsProvider(configuration));

        var settingsProvider = new AppSettingsProvider(configuration);
        var producerOptions = settingsProvider.GetSection<ChatMessageSentProducerSettingsSection>();

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
