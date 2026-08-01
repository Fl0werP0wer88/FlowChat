using FlowChat.AuthService.Application;
using FlowChat.AuthService.Consumers.Configuration.Settings;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using FlowChat.AuthService.Persistence;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.AuthService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(new UserEmailConfirmedConsumerSettingsSection().SectionName)
            .Get<UserEmailConfirmedConsumerSettingsSection>()
            ?? new UserEmailConfirmedConsumerSettingsSection();
        var accountConfirmedOptions = configuration
            .GetSection(new AccountConfirmedProducerSettingsSection().SectionName)
            .Get<AccountConfirmedProducerSettingsSection>()
            ?? new AccountConfirmedProducerSettingsSection();

        services.AddConsumerApplicationServices();
        services.AddConsumerInfrastructureServices(configuration);
        services.AddConsumerPersistenceServices(configuration);

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddFlowChatTieredRetryConsumerPipeline<AppDbContext>(
                consumerOptions.BootstrapServers,
                [consumerOptions])
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkOutbox())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(consumerOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<AccountConfirmedIntegrationEvent>("auth-account-confirmed", endpoint => endpoint
                            .ProduceTo(accountConfirmedOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            })
            .AddScopedSubscriber<UserEmailConfirmedSubscriber>()
            .AddScopedSubscriber<AuthEmailChangedSubscriber>();

        return services;
    }
}
