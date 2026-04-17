using FlowChat.AuthService.OutboxPublisher.Configuration;
using FlowChat.AuthService.Persistence;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.AuthService.OutboxPublisher;

public static class OutboxPublisherServiceRegistration
{
    public static IServiceCollection AddOutboxPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var outboxOptions = configuration
            .GetSection(OutboxPublisherRuntimeSettingsSection.SectionName)
            .Get<OutboxPublisherRuntimeSettingsSection>()
            ?? new OutboxPublisherRuntimeSettingsSection();
        var accountRegisteredOptions = configuration
            .GetSection(AccountRegisteredProducerSettingsSection.SectionName)
            .Get<AccountRegisteredProducerSettingsSection>()
            ?? new AccountRegisteredProducerSettingsSection();

        services.AddOptions<OutboxPublisherRuntimeSettingsSection>()
            .BindConfiguration(OutboxPublisherRuntimeSettingsSection.SectionName);
        services.AddOptions<AccountRegisteredProducerSettingsSection>()
            .BindConfiguration(AccountRegisteredProducerSettingsSection.SectionName);

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
                options.AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())
                    .WithBatchSize(outboxOptions.BatchSize)
                    .WithInterval(TimeSpan.FromSeconds(outboxOptions.PollIntervalSeconds))
                    .WithExponentialRetryDelay(
                        TimeSpan.FromSeconds(outboxOptions.RetryBaseDelaySeconds),
                        2,
                        TimeSpan.FromSeconds(outboxOptions.MaxRetryDelaySeconds))
                    .WithoutDistributedLock());
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(accountRegisteredOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<AccountRegisteredIntegrationEvent>("auth-account-registered", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<AccountConfirmedIntegrationEvent>("auth-account-confirmed", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<PhoneNumberConfirmedIntegrationEvent>("auth-user-phone-confirmed", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }
}


