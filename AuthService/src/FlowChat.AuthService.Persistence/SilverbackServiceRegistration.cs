using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.Messaging.Runtime.Kafka.GenericProducer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;

namespace FlowChat.AuthService.Persistence;

public static class SilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var userCreatedOptions = configuration.GetSection(UserCreatedProducerOptions.SectionName)
            .Get<UserCreatedProducerOptions>() ?? new UserCreatedProducerOptions();
        var emailVerificationOptions = configuration.GetSection(UserEmailVerificationRequestedOutboxOptions.SectionName)
            .Get<UserEmailVerificationRequestedOutboxOptions>() ?? new UserEmailVerificationRequestedOutboxOptions();

        services.AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(userCreatedOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<UserCreatedIntegrationEvent>(endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => userCreatedOptions.KeySelector(message!))
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<UserConfirmedIntegrationEvent>(endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message!.UserId.ToString())
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<UserEmailVerificationRequestedIntegrationEvent>(endpoint => endpoint
                            .ProduceTo(emailVerificationOptions.Topic)
                            .SetKafkaKey(message => emailVerificationOptions.KeySelector(message!))
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }

    public static IServiceCollection AddWorkerSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var outboxOptions = configuration.GetSection(OutboxPublisherRuntimeOptions.SectionName)
            .Get<OutboxPublisherRuntimeOptions>() ?? new OutboxPublisherRuntimeOptions();
        var userCreatedOptions = configuration.GetSection(UserCreatedProducerOptions.SectionName)
            .Get<UserCreatedProducerOptions>()
            ?? configuration.GetSection(UserCreatedProducerOptions.FallbackSectionName)
                .Get<UserCreatedProducerOptions>()
            ?? new UserCreatedProducerOptions();
        var emailVerificationOptions = configuration.GetSection(UserEmailVerificationRequestedOutboxOptions.SectionName)
            .Get<UserEmailVerificationRequestedOutboxOptions>()
            ?? configuration.GetSection(UserEmailVerificationRequestedOutboxOptions.FallbackSectionName)
                .Get<UserEmailVerificationRequestedOutboxOptions>()
            ?? new UserEmailVerificationRequestedOutboxOptions();

        services.Configure<OutboxPublisherRuntimeOptions>(
            configuration.GetSection(OutboxPublisherRuntimeOptions.SectionName));

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
                clients.WithBootstrapServers(outboxOptions.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<UserCreatedIntegrationEvent>(endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => userCreatedOptions.KeySelector(message!))
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<UserConfirmedIntegrationEvent>(endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message!.UserId.ToString())
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<UserEmailVerificationRequestedIntegrationEvent>(endpoint => endpoint
                            .ProduceTo(emailVerificationOptions.Topic)
                            .SetKafkaKey(message => emailVerificationOptions.KeySelector(message!))
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }
}
