using FlowChat.AuthService.Persistence;
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
        var outboxSection = configuration.GetSection("MassTransit:Outbox");
        var queryDelaySeconds = outboxSection.GetValue<int?>("QueryDelaySeconds");
        var queryMessageLimit = outboxSection.GetValue<int?>("QueryMessageLimit");
        var queryTimeoutSeconds = outboxSection.GetValue<int?>("QueryTimeoutSeconds");
        var duplicateDetectionWindowSeconds = outboxSection.GetValue<int?>("DuplicateDetectionWindowSeconds");
        var messageDeliveryLimit = outboxSection.GetValue<int?>("MessageDeliveryLimit");
        var concurrentDeliveryLimit = outboxSection.GetValue<int?>("ConcurrentDeliveryLimit");
        var messageDeliveryTimeoutSeconds = outboxSection.GetValue<int?>("MessageDeliveryTimeoutSeconds");
        var disableDeliveryService = outboxSection.GetValue("DisableDeliveryService", true);

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
        });

        return services;
    }
}
