using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using MediatR;
using Silverback.Messaging.Subscribers;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileProjectionBatchSubscriber(
    IMediator mediator,
    IPublisher publisher,
    IMapper mapper,
    ILogger<UserProfileProjectionBatchSubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.UserProfileMainConsumerName)]
    public async Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<UserProfileReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<UserProfileProjectionCommandItem>();
        var originalMessages = new List<ProjectionIntegrationEvent<UserProfileReadModel>>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            originalMessages.Add(message);
            items.Add(UserProfileSubscriberHelper.MapProjectionEvent(message, mapper));
        }

        if (items.Count == 0)
        {
            logger.LogDebug("Skipping empty user profile projection batch.");
            return;
        }

        var deduplicatedItems = UserProfileSubscriberHelper.KeepLastItemPerUserProfile(items);
        var result = await mediator.Send(
            new BulkUpsertOrDeleteUserProfileProjectionCommand(deduplicatedItems),
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Processed {Count} user profile projection events from Kafka batch.",
                deduplicatedItems.Count);
            return;
        }

        if (result.Error.FailureKind != FailureKind.Isolable)
            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

        logger.LogWarning(
            "User profile projection batch failed with an isolable error; republishing {Count} events to retry topic.",
            originalMessages.Count);

        foreach (var message in originalMessages)
        {
            await publisher.PublishAsync(message, cancellationToken);
        }
    }
}
