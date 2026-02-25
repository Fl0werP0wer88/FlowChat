using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.Messaging.Runtime.Kafka.GenericConsumer;
using FlowChat.NotificationService.Application.Notifications.Commands;
using FlowChat.NotificationService.Worker.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.NotificationService.Worker;

public static class WorkerServiceRegistration
{
    public static IServiceCollection AddWorkerKafkaConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UserCreatedConsumerOptions>(
            configuration.GetSection(UserCreatedConsumerOptions.SectionName));

        services.AddOptions<KafkaConsumerRuntimeOptions>()
            .Configure<IOptions<UserCreatedConsumerOptions>>((runtime, source) =>
            {
                runtime.BootstrapServers = source.Value.BootstrapServers;
                runtime.GroupId = source.Value.GroupId;
                runtime.AutoOffsetReset = source.Value.AutoOffsetReset;
            });

        services.AddSingleton<ITopicSubscription>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<UserCreatedConsumerOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("NotificationUserCreatedTopicSubscription");

            return new TopicSubscription<UserCreatedIntegrationEvent>(
                topic: options.Topic,
                retryTopic: options.RetryTopic,
                deadLetterTopic: options.DeadLetterTopic,
                maxRetryCount: options.MaxRetryCount,
                handler: async (message, context, scopedProvider, cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(message.Email))
                    {
                        return MessageHandlingResult.Skip("Payload does not contain Email.");
                    }

                    if (string.IsNullOrWhiteSpace(message.UserName))
                    {
                        return MessageHandlingResult.Skip("Payload does not contain UserName.");
                    }

                    var userId = ResolveUserId(message.UserId, context.Key);
                    if (!userId.HasValue)
                    {
                        return MessageHandlingResult.Skip("Payload does not contain valid UserId.");
                    }

                    var displayName = string.IsNullOrWhiteSpace(message.DisplayName)
                        ? message.UserName.Trim()
                        : message.DisplayName.Trim();

                    var mediator = scopedProvider.GetRequiredService<IMediator>();

                    try
                    {
                        await mediator.Send(
                            new HandleUserCreatedNotificationCommand(
                                userId.Value,
                                message.Email,
                                message.UserName,
                                displayName,
                                context.Key),
                            cancellationToken);

                        return MessageHandlingResult.Success();
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogInformation(
                            ex,
                            "Skipping notification handling for key {Key}. Reason: {Reason}",
                            context.Key,
                            ex.Message);

                        return MessageHandlingResult.Skip(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Transient failure while handling notification for key {Key}.",
                            context.Key);

                        return MessageHandlingResult.Retry(ex.Message);
                    }
                });
        });

        services.AddHostedService<GenericKafkaConsumerBackgroundService>();

        return services;
    }

    private static Guid? ResolveUserId(Guid payloadUserId, string? key)
    {
        if (payloadUserId != Guid.Empty)
        {
            return payloadUserId;
        }

        return Guid.TryParse(key, out var keyAsGuid)
            ? keyAsGuid
            : null;
    }
}
