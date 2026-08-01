using FlowChat.AuthService.OutboxPublisher.Configuration.Settings;
using FlowChat.AuthService.Persistence;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
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
            .GetSection(new OutboxPublisherRuntimeSettingsSection().SectionName)
            .Get<OutboxPublisherRuntimeSettingsSection>()
            ?? new OutboxPublisherRuntimeSettingsSection();
        var accountRegisteredOptions = configuration
            .GetSection(new AccountRegisteredProducerSettingsSection().SectionName)
            .Get<AccountRegisteredProducerSettingsSection>()
            ?? new AccountRegisteredProducerSettingsSection();
        var accountConfirmedOptions = configuration
            .GetSection(new AccountConfirmedProducerSettingsSection().SectionName)
            .Get<AccountConfirmedProducerSettingsSection>()
            ?? new AccountConfirmedProducerSettingsSection();
        var phoneNumberConfirmedOptions = configuration
            .GetSection(new PhoneNumberConfirmedProducerSettingsSection().SectionName)
            .Get<PhoneNumberConfirmedProducerSettingsSection>()
            ?? new PhoneNumberConfirmedProducerSettingsSection();
        var retryKafkaOptions = configuration
            .GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>()
            ?? new RetryOutboxKafkaSettingsSection();
        var bootstrapServers = !string.IsNullOrWhiteSpace(accountRegisteredOptions.BootstrapServers)
            ? accountRegisteredOptions.BootstrapServers
            : !string.IsNullOrWhiteSpace(accountConfirmedOptions.BootstrapServers)
                ? accountConfirmedOptions.BootstrapServers
                : phoneNumberConfirmedOptions.BootstrapServers;

        services.AddOptions<OutboxPublisherRuntimeSettingsSection>()
            .BindConfiguration(new OutboxPublisherRuntimeSettingsSection().SectionName);
        services.AddOptions<AccountRegisteredProducerSettingsSection>()
            .BindConfiguration(new AccountRegisteredProducerSettingsSection().SectionName);
        services.AddOptions<AccountConfirmedProducerSettingsSection>()
            .BindConfiguration(new AccountConfirmedProducerSettingsSection().SectionName);
        services.AddOptions<PhoneNumberConfirmedProducerSettingsSection>()
            .BindConfiguration(new PhoneNumberConfirmedProducerSettingsSection().SectionName);
        services.AddOptions<RetryOutboxKafkaSettingsSection>()
            .BindConfiguration(new RetryOutboxKafkaSettingsSection().SectionName);

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
                options.AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())
                    .WithBatchSize(outboxOptions.BatchSize)
                    .WithInterval(outboxOptions.PollInterval)
                    .WithExponentialRetryDelay(
                        TimeSpan.FromSeconds(outboxOptions.RetryBaseDelaySeconds),
                        2,
                        TimeSpan.FromSeconds(outboxOptions.MaxRetryDelaySeconds))
                    .WithoutDistributedLock());
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(bootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<AccountRegisteredIntegrationEvent>("auth-account-registered", endpoint => endpoint
                            .ProduceTo(accountRegisteredOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<AccountConfirmedIntegrationEvent>("auth-account-confirmed", endpoint => endpoint
                            .ProduceTo(accountConfirmedOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<PhoneNumberConfirmedIntegrationEvent>("auth-user-phone-confirmed", endpoint => endpoint
                            .ProduceTo(phoneNumberConfirmedOptions.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddFlowChatTieredRetryProducerPipeline(
                retryKafkaOptions.BootstrapServers,
                retryKafkaOptions.Topics);

        return services;
    }
}


