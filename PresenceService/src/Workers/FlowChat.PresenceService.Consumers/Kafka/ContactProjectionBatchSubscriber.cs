using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.Shared.Domain;
using MediatR;
using Silverback.Messaging.Subscribers;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class ContactProjectionBatchSubscriber(
    IMediator mediator,
    IPublisher publisher,
    ILogger<ContactProjectionBatchSubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ContactMainConsumerName)]
    public async Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<ContactReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<UserContactProjectionCommandItem>();
        var originalMessages = new List<ProjectionIntegrationEvent<ContactReadModel>>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            originalMessages.Add(message);
            items.Add(ContactProjectionSubscriberHelper.MapProjectionEvent(message));
        }

        if (items.Count == 0)
        {
            logger.LogDebug("Skipping empty contact projection batch.");
            return;
        }

        var deduplicatedItems = ContactProjectionSubscriberHelper.KeepLastItemPerContactObserver(items);
        var result = await mediator.Send(
            new BulkUpsertOrDeleteUserContactProjectionCommand(deduplicatedItems),
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Processed {Count} contact projection events from Kafka batch.",
                deduplicatedItems.Count);
            return;
        }

        if (result.Error.FailureKind != FailureKind.Isolable)
            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

        logger.LogWarning(
            "Contact projection batch failed with an isolable error; republishing {Count} events to retry topic.",
            originalMessages.Count);

        foreach (var message in originalMessages)
        {
            await publisher.PublishAsync(message, cancellationToken);
        }
    }
}
