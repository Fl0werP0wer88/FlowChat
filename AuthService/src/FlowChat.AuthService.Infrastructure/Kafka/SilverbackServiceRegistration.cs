using FlowChat.AuthService.Persistence;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public static class SilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsManager = new WorkerSettingsManager(configuration);
        services.TryAddSingleton<IWorkerSettingsManager>(settingsManager);

        var accountRegisteredOptions = settingsManager.GetAccountRegisteredProducerOptions();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(accountRegisteredOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<AccountRegisteredIntegrationEvent>("auth-account-registered", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<UserConfirmedIntegrationEvent>("auth-user-confirmed", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<PhoneNumberConfirmedIntegrationEvent>("auth-user-phone-confirmed", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }

}


