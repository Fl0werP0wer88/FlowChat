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
        var settings = ResolveSettings(configuration);

        services.AddMassTransit(configurator =>
        {
            configurator.AddEntityFrameworkOutbox<AppDbContext>(outbox =>
            {
                ConfigureOutbox(outbox, settings, disableDeliveryService: true);
            });

            ConfigureBusAndKafka(configurator, settings);
        });

        return services;
    }

    public static IServiceCollection AddWorkerMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = ResolveSettings(configuration);

        services.AddMassTransit(configurator =>
        {
            configurator.AddEntityFrameworkOutbox<AppDbContext>(outbox =>
            {
                ConfigureOutbox(outbox, settings, disableDeliveryService: false);
            });

            ConfigureBusAndKafka(configurator, settings);
        });

        return services;
    }

    private static void ConfigureBusAndKafka(IBusRegistrationConfigurator configurator, MassTransitSettings settings)
    {
        configurator.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));

        configurator.AddRider(rider =>
        {
            rider.AddProducer<string, AuthIdentityTopicEventBase>(settings.UserCreatedOptions.Topic);

            rider.UsingKafka((_, kafka) =>
            {
                kafka.Host(settings.UserCreatedOptions.BootstrapServers);
            });
        });
    }

    private static void ConfigureOutbox(
        IEntityFrameworkOutboxConfigurator outbox,
        MassTransitSettings settings,
        bool disableDeliveryService)
    {
        outbox.UsePostgres();

        if (settings.QueryDelaySeconds.HasValue)
        {
            outbox.QueryDelay = TimeSpan.FromSeconds(Math.Max(1, settings.QueryDelaySeconds.Value));
        }

        if (settings.QueryMessageLimit.HasValue)
        {
            outbox.QueryMessageLimit = Math.Max(1, settings.QueryMessageLimit.Value);
        }

        if (settings.QueryTimeoutSeconds.HasValue)
        {
            outbox.QueryTimeout = TimeSpan.FromSeconds(Math.Max(1, settings.QueryTimeoutSeconds.Value));
        }

        if (settings.DuplicateDetectionWindowSeconds.HasValue)
        {
            outbox.DuplicateDetectionWindow = TimeSpan.FromSeconds(
                Math.Max(1, settings.DuplicateDetectionWindowSeconds.Value));
        }

        outbox.UseBusOutbox(busOutbox =>
        {
            if (settings.MessageDeliveryLimit.HasValue)
            {
                busOutbox.MessageDeliveryLimit = Math.Max(1, settings.MessageDeliveryLimit.Value);
            }

            if (settings.ConcurrentDeliveryLimit.HasValue)
            {
                busOutbox.ConcurrentDeliveryLimit = Math.Max(1, settings.ConcurrentDeliveryLimit.Value);
            }

            if (settings.MessageDeliveryTimeoutSeconds.HasValue)
            {
                busOutbox.MessageDeliveryTimeout = TimeSpan.FromSeconds(
                    Math.Max(1, settings.MessageDeliveryTimeoutSeconds.Value));
            }

            if (disableDeliveryService)
            {
                busOutbox.DisableDeliveryService();
            }
        });
    }

    private static MassTransitSettings ResolveSettings(IConfiguration configuration)
    {
        var userCreatedOptions = ResolveUserCreatedProducerOptions(configuration);

        var outboxSection = configuration.GetSection("MassTransit:Outbox");
        return new MassTransitSettings
        {
            UserCreatedOptions = userCreatedOptions,
            QueryDelaySeconds = outboxSection.GetValue<int?>("QueryDelaySeconds"),
            QueryMessageLimit = outboxSection.GetValue<int?>("QueryMessageLimit"),
            QueryTimeoutSeconds = outboxSection.GetValue<int?>("QueryTimeoutSeconds"),
            DuplicateDetectionWindowSeconds = outboxSection.GetValue<int?>("DuplicateDetectionWindowSeconds"),
            MessageDeliveryLimit = outboxSection.GetValue<int?>("MessageDeliveryLimit"),
            ConcurrentDeliveryLimit = outboxSection.GetValue<int?>("ConcurrentDeliveryLimit"),
            MessageDeliveryTimeoutSeconds = outboxSection.GetValue<int?>("MessageDeliveryTimeoutSeconds")
        };
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

    private sealed class KafkaProducerSettings
    {
        public string BootstrapServers { get; init; } = "localhost:9092";
        public string Topic { get; init; } = "dev.flowchat.identity.user.v1";
    }

    private sealed class MassTransitSettings
    {
        public KafkaProducerSettings UserCreatedOptions { get; init; } = new();
        public int? QueryDelaySeconds { get; init; }
        public int? QueryMessageLimit { get; init; }
        public int? QueryTimeoutSeconds { get; init; }
        public int? DuplicateDetectionWindowSeconds { get; init; }
        public int? MessageDeliveryLimit { get; init; }
        public int? ConcurrentDeliveryLimit { get; init; }
        public int? MessageDeliveryTimeoutSeconds { get; init; }
    }
}
