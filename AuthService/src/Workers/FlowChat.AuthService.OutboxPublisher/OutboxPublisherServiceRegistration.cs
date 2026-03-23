using FlowChat.AuthService.OutboxPublisher.Configuration;
using FlowChat.AuthService.Persistence;
using FlowChat.Messaging.Contracts.AuthService.Events;
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
            .GetSection(OutboxPublisherRuntimeOptions.SectionName)
            .Get<OutboxPublisherRuntimeOptions>()
            ?? new OutboxPublisherRuntimeOptions();
        var userCreatedOptions = configuration
            .GetSection(UserCreatedProducerOptions.SectionName)
            .Get<UserCreatedProducerOptions>()
            ?? new UserCreatedProducerOptions();
        var emailVerificationOptions = configuration
            .GetSection(UserEmailVerificationRequestedProducerOptions.SectionName)
            .Get<UserEmailVerificationRequestedProducerOptions>()
            ?? new UserEmailVerificationRequestedProducerOptions();

        services.AddOptions<OutboxPublisherRuntimeOptions>()
            .BindConfiguration(OutboxPublisherRuntimeOptions.SectionName);
        services.AddOptions<UserCreatedProducerOptions>()
            .BindConfiguration(UserCreatedProducerOptions.SectionName);
        services.AddOptions<UserEmailVerificationRequestedProducerOptions>()
            .BindConfiguration(UserEmailVerificationRequestedProducerOptions.SectionName);

        services.AddSilverback()
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
                clients.WithBootstrapServers(userCreatedOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<UserCreatedIntegrationEvent>("auth-user-created", endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<UserConfirmedIntegrationEvent>("auth-user-confirmed", endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<EmailConfirmedIntegrationEvent>("auth-user-email-confirmed", endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<PhoneNumberConfirmedIntegrationEvent>("auth-user-phone-confirmed", endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<EmailVerificationRequestIntegrationEvent>("auth-user-email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }
}
