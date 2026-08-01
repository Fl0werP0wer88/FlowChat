using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.UserProfileService.Application;
using FlowChat.UserProfileService.Consumers.Configuration.Settings;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using FlowChat.UserProfileService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.UserProfileService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(new AccountRegisteredConsumerSettingsSection().SectionName)
            .Get<AccountRegisteredConsumerSettingsSection>()
            ?? new AccountRegisteredConsumerSettingsSection();
        var emailVerificationRequestedProducerOptions = configuration
            .GetSection(new UserEmailVerificationRequestedProducerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedProducerSettingsSection>()
            ?? new UserEmailVerificationRequestedProducerSettingsSection();
        var projectionProducerOptions = configuration
            .GetSection(new UserProfileProjectionProducerSettingsSection().SectionName)
            .Get<UserProfileProjectionProducerSettingsSection>()
            ?? new UserProfileProjectionProducerSettingsSection();

        services.AddConsumerApplicationServices();
        services.AddConsumerInfrastructureServices(configuration);
        services.AddConsumerPersistenceServices(configuration);
        services.AddDataProtection()
            .PersistKeysToDbContext<AppDbContext>()
            .SetApplicationName("FlowChat.UserProfileService");

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddFlowChatTieredRetry<AppDbContext>(
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
                        .Produce<EmailVerificationRequestIntegrationEvent>("email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationRequestedProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<UserProfileReadModel>>("user-profile-projection", endpoint => endpoint
                            .ProduceTo(projectionProducerOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            })
            .AddScopedSubscriber<AccountRegisteredSubscriber>();

        return services;
    }
}


