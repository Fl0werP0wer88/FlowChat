using FlowChat.UserProfileService.Worker.Kafka;
using FlowChat.Messaging.Contracts.AuthService.Events;
using FlowChat.Messaging.Runtime.Kafka.GenericConsumer;
using FlowChat.UserProfileService.Application.UserProfiles.Commands;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.Worker;

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
                .CreateLogger("UserCreatedTopicSubscription");

            return new TopicSubscription<UserCreatedIntegrationEvent>(
                topic: options.Topic,
                retryTopic: options.RetryTopic,
                deadLetterTopic: options.DeadLetterTopic,
                maxRetryCount: options.MaxRetryCount,
                handler: async (message, context, scopedProvider, cancellationToken) =>
                {
                    var userName = message.UserName?.Trim();
                    if (string.IsNullOrWhiteSpace(userName))
                    {
                        return MessageHandlingResult.Skip("Payload does not contain UserName.");
                    }

                    var userId = ResolveUserId(message.UserId, context.Key);
                    if (!userId.HasValue)
                    {
                        return MessageHandlingResult.Skip("Payload does not contain valid UserId.");
                    }

                    var displayName = ResolveDisplayName(message, userName);
                    var mediator = scopedProvider.GetRequiredService<IMediator>();

                    try
                    {
                        await mediator.Send(
                            new CreateInitialUserProfileCommand(
                                userName,
                                displayName,
                                null,
                                null,
                                userId.Value),
                            cancellationToken);

                        return MessageHandlingResult.Success();
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogInformation(
                            ex,
                            "Skipping user profile creation for key {Key}. Reason: {Reason}",
                            context.Key,
                            ex.Message);

                        return MessageHandlingResult.Skip(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Transient failure while creating user profile for key {Key}.",
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

    private static string ResolveDisplayName(UserCreatedIntegrationEvent message, string userName)
    {
        if (!string.IsNullOrWhiteSpace(message.DisplayName))
        {
            return message.DisplayName.Trim();
        }

        var firstName = message.FirstName?.Trim();
        var lastName = message.LastName?.Trim();
        var fullName = $"{firstName} {lastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName)
            ? userName
            : fullName;
    }
}

