using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public abstract class ProjectionBatchSubscriberBase<TReadModel, TItem>(
    IMediator mediator,
    IPublisher publisher,
    IProjectionValueFactory<TReadModel, TItem> valueFactory,
    ILogger logger)
    where TReadModel : class
    where TItem : class
{
    protected async Task HandleBatchAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<TReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<ProjectionCommandItem<TItem>>();
        var originalMessages = new List<ProjectionIntegrationEvent<TReadModel>>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            ValidateMessage(message);

            originalMessages.Add(message);
            items.Add(MapItem(message));
        }

        if (items.Count == 0)
        {
            logger.LogDebug("Skipping empty projection batch.");
            return;
        }

        var result = await mediator.Send(new ProjectionBulkCommand<ProjectionCommandItem<TItem>>(items), cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Processed {Count} projection events from Kafka batch.", items.Count);
            return;
        }

        if (result.Error.FailureKind != FailureKind.Isolable)
            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

        // Silverback's MoveMessageErrorPolicy cannot move BatchSequence messages to retry topic
        await RepublishToRetryAsync(originalMessages, cancellationToken);
    }

    private async Task RepublishToRetryAsync(
        IReadOnlyCollection<ProjectionIntegrationEvent<TReadModel>> messages,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Batch upsert failed with an isolable error; republishing {Count} events to retry topic.",
            messages.Count);

        foreach (var message in messages)
        {
            await publisher.PublishAsync(message, cancellationToken);
        }
    }

    private static void ValidateMessage(ProjectionIntegrationEvent<TReadModel> message)
    {
        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");
    }

    private ProjectionCommandItem<TItem> MapItem(ProjectionIntegrationEvent<TReadModel> message) =>
        new(
            message.SourceAggregateId,
            message.Operation == OperationType.Deleted
                ? null
                : valueFactory.MapValue(message),
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);
}
