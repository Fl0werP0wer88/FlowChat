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
                runtime.RetryBaseDelaySeconds = source.Value.RetryBaseDelaySeconds;
                runtime.RetryMaxDelaySeconds = source.Value.RetryMaxDelaySeconds;
            });

        services.AddSingleton<ITopicSubscription>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<UserCreatedConsumerOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("NotificationUserEmailVerificationRequestedTopicSubscription");

            return new TopicSubscription<UserEmailVerificationRequestedIntegrationEvent>(
                topic: options.Topic,
                retryTopic: options.RetryTopic,
                deadLetterTopic: options.DeadLetterTopic,
                maxRetryCount: options.MaxRetryCount,
                handler: async (message, context, scopedProvider, cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(message.UserEmail))
                    {
                        return MessageHandlingResult.Skip("Payload does not contain UserEmail.");
                    }

                    var userName = ResolveUserName(message.UserEmail);
                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        return MessageHandlingResult.Skip("Payload does not contain valid UserEmail local-part.");
                    }

                    var userId = ResolveUserId(message.UserId, context.Key);
                    if (!userId.HasValue)
                    {
                        return MessageHandlingResult.Skip("Payload does not contain valid UserId.");
                    }

                    var mediator = scopedProvider.GetRequiredService<IMediator>();

                    try
                    {
                        await mediator.Send(
                            new UserEmailVerificationRequestedCommand(
                                userId.Value,
                                message.UserEmail.Trim(),
                                userName,
                                userName,
                                message.ConfirmationLink,
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

    private static string? ResolveUserName(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var value = email.Trim();
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0)
        {
            return null;
        }

        return value[..atIndex].Trim();
    }
}
