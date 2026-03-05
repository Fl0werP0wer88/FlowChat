using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence.Configuration;
using FlowChat.Messaging.Contracts.AuthService.Events;

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
        var userCreatedOptions = ResolveUserCreatedProducerOptions(configuration);
        var emailVerificationOptions = ResolveEmailVerificationProducerOptions(configuration);

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
                        .Produce<UserCreatedIntegrationEvent>("auth-user-created", endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<UserConfirmedIntegrationEvent>("auth-user-confirmed", endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<EmailVerificationRequestIntegrationEvent>("auth-user-email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
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
        var userCreatedOptions = ResolveUserCreatedProducerOptions(configuration);
        var emailVerificationOptions = ResolveEmailVerificationProducerOptions(configuration);

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
                        .Produce<EmailVerificationRequestIntegrationEvent>("auth-user-email-verification-requested", endpoint => endpoint
                            .ProduceTo(emailVerificationOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            });

        return services;
    }

    private static KafkaProducerSettings ResolveUserCreatedProducerOptions(IConfiguration configuration)
    {
        var producerSection = configuration.GetSection(UserCreatedProducerOptions.SectionName);
        var fallbackSection = configuration.GetSection(UserCreatedProducerOptions.FallbackSectionName);

        return new KafkaProducerSettings
        {
            BootstrapServers = producerSection["BootstrapServers"]
                ?? fallbackSection["BootstrapServers"]
                ?? "localhost:9092",
            Topic = producerSection["Topic"]
                ?? fallbackSection["Topic"]
                ?? "dev.flowchat.identity.user.v1"
        };
    }

    private static KafkaProducerSettings ResolveEmailVerificationProducerOptions(IConfiguration configuration)
    {
        var producerSection = configuration.GetSection(UserEmailVerificationRequestedProducerOptions.SectionName);
        var fallbackSection = configuration.GetSection(UserEmailVerificationRequestedProducerOptions.FallbackSectionName);

        return new KafkaProducerSettings
        {
            BootstrapServers = producerSection["BootstrapServers"]
                ?? fallbackSection["BootstrapServers"]
                ?? "localhost:9092",
            Topic = producerSection["Topic"]
                ?? fallbackSection["Topic"]
                ?? "dev.flowchat.identity.user.v1"
        };
    }

    private sealed class KafkaProducerSettings
    {
        public string BootstrapServers { get; init; } = "localhost:9092";
        public string Topic { get; init; } = "dev.flowchat.identity.user.v1";
    }
}
