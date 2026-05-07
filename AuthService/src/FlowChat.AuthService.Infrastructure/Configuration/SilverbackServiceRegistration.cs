using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var accountRegisteredOptions = configuration.GetSection(new AccountRegisteredProducerSettingsSection().SectionName)
            .Get<AccountRegisteredProducerSettingsSection>() ?? new AccountRegisteredProducerSettingsSection();
        var accountConfirmedOptions = configuration.GetSection(new AccountConfirmedProducerSettingsSection().SectionName)
            .Get<AccountConfirmedProducerSettingsSection>() ?? new AccountConfirmedProducerSettingsSection();
        var phoneNumberConfirmedOptions = configuration.GetSection(new PhoneNumberConfirmedProducerSettingsSection().SectionName)
            .Get<PhoneNumberConfirmedProducerSettingsSection>() ?? new PhoneNumberConfirmedProducerSettingsSection();
        var bootstrapServers = !string.IsNullOrWhiteSpace(accountRegisteredOptions.BootstrapServers)
            ? accountRegisteredOptions.BootstrapServers
            : !string.IsNullOrWhiteSpace(accountConfirmedOptions.BootstrapServers)
                ? accountConfirmedOptions.BootstrapServers
                : phoneNumberConfirmedOptions.BootstrapServers;

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(bootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<AccountRegisteredIntegrationEvent>("auth-account-registered", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<AccountConfirmedIntegrationEvent>("auth-account-confirmed", endpoint => endpoint
                            .ProduceTo(accountConfirmedOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<PhoneNumberConfirmedIntegrationEvent>("auth-user-phone-confirmed", endpoint => endpoint
                            .ProduceTo(phoneNumberConfirmedOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}
