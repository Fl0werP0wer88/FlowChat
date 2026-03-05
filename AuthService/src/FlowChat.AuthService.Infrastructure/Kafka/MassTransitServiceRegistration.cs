using FlowChat.AuthService.Persistence;
using FlowChat.Messaging.Contracts.AuthService.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.Infrastructure.Kafka;

public static class MassTransitServiceRegistration
{
    public static IServiceCollection AddApiMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var userCreatedOptions = ResolveUserCreatedProducerOptions(configuration);
        var emailVerificationOptions = ResolveEmailVerificationProducerOptions(configuration);

        if (!string.Equals(
                userCreatedOptions.BootstrapServers,
                emailVerificationOptions.BootstrapServers,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "MassTransit Kafka rider requires a single BootstrapServers value for all producers in this registration.");
        }

        var outboxSection = configuration.GetSection("MassTransit:Outbox");
        var queryDelaySeconds = outboxSection.GetValue<int?>("QueryDelaySeconds");
        var queryMessageLimit = outboxSection.GetValue<int?>("QueryMessageLimit");
        var queryTimeoutSeconds = outboxSection.GetValue<int?>("QueryTimeoutSeconds");
        var duplicateDetectionWindowSeconds = outboxSection.GetValue<int?>("DuplicateDetectionWindowSeconds");
        var messageDeliveryLimit = outboxSection.GetValue<int?>("MessageDeliveryLimit");
        var concurrentDeliveryLimit = outboxSection.GetValue<int?>("ConcurrentDeliveryLimit");
        var messageDeliveryTimeoutSeconds = outboxSection.GetValue<int?>("MessageDeliveryTimeoutSeconds");
        var disableDeliveryService = outboxSection.GetValue("DisableDeliveryService", false);

        services.AddMassTransit(configurator =>
        {
            configurator.AddEntityFrameworkOutbox<AppDbContext>(outbox =>
            {
                outbox.UsePostgres();

                if (queryDelaySeconds.HasValue)
                {
                    outbox.QueryDelay = TimeSpan.FromSeconds(Math.Max(1, queryDelaySeconds.Value));
                }

                if (queryMessageLimit.HasValue)
                {
                    outbox.QueryMessageLimit = Math.Max(1, queryMessageLimit.Value);
                }

                if (queryTimeoutSeconds.HasValue)
                {
                    outbox.QueryTimeout = TimeSpan.FromSeconds(Math.Max(1, queryTimeoutSeconds.Value));
                }

                if (duplicateDetectionWindowSeconds.HasValue)
                {
                    outbox.DuplicateDetectionWindow = TimeSpan.FromSeconds(
                        Math.Max(1, duplicateDetectionWindowSeconds.Value));
                }

                outbox.UseBusOutbox(busOutbox =>
                {
                    if (messageDeliveryLimit.HasValue)
                    {
                        busOutbox.MessageDeliveryLimit = Math.Max(1, messageDeliveryLimit.Value);
                    }

                    if (concurrentDeliveryLimit.HasValue)
                    {
                        busOutbox.ConcurrentDeliveryLimit = Math.Max(1, concurrentDeliveryLimit.Value);
                    }

                    if (messageDeliveryTimeoutSeconds.HasValue)
                    {
                        busOutbox.MessageDeliveryTimeout = TimeSpan.FromSeconds(
                            Math.Max(1, messageDeliveryTimeoutSeconds.Value));
                    }

                    if (disableDeliveryService)
                    {
                        busOutbox.DisableDeliveryService();
                    }
                });
            });

            configurator.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));

            configurator.AddRider(rider =>
            {
                rider.AddProducer<string, UserCreatedIntegrationEvent>(userCreatedOptions.Topic);
                rider.AddProducer<string, UserConfirmedIntegrationEvent>(userCreatedOptions.Topic);
                rider.AddProducer<string, EmailVerificationRequestIntegrationEvent>(emailVerificationOptions.Topic);

                rider.UsingKafka((_, kafka) =>
                {
                    kafka.Host(userCreatedOptions.BootstrapServers);
                });
            });
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
