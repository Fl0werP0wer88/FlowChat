using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.Configuration;
using FlowChat.Messaging.Contracts.AuthService.Events;
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

        var userCreatedOptions = settingsManager.GetUserCreatedProducerOptions();
        var emailVerificationOptions = settingsManager.GetUserEmailVerificationRequestedProducerOptions();

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
                        .Produce<EmailConfirmedIntegrationEvent>("auth-user-email-confirmed", endpoint => endpoint
                            .ProduceTo(userCreatedOptions.Topic)
                            .SetKafkaKey(message => message?.UserId)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<PhoneNumberConfirmedIntegrationEvent>("auth-user-phone-confirmed", endpoint => endpoint
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
        var settingsManager = new WorkerSettingsManager(configuration);
        services.TryAddSingleton<IWorkerSettingsManager>(settingsManager);

        var outboxOptions = settingsManager.GetOutboxPublisherRuntimeOptions();
        var userCreatedOptions = settingsManager.GetUserCreatedProducerOptions();
        var emailVerificationOptions = settingsManager.GetUserEmailVerificationRequestedProducerOptions();

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
