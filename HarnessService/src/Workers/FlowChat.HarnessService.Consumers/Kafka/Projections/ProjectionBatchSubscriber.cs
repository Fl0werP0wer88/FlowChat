using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.Shared.Domain;
using MediatR;
using Silverback.Messaging.Subscribers;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionBatchSubscriber(
    IMediator mediator,
    IPublisher publisher,
    ILogger<ProjectionBatchSubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ProjectionMainConsumerName)]
    public async Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<ProjectionTestReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<ProjectionCommandItem>();
        var originalMessages = new List<ProjectionIntegrationEvent<ProjectionTestReadModel>>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            if (message.SourceAggregateVersion <= 0)
                throw new NonTransientException("Payload does not contain valid SourceVersion.");

            originalMessages.Add(message);
            items.Add(MapEvent(message));
        }

        if (items.Count == 0)
        {
            logger.LogDebug("Skipping empty projection batch.");
            return;
        }

        var result = await mediator.Send(new BulkUpsertProjectionCommand(items), cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Processed {Count} projection events from Kafka batch.", items.Count);
            return;
        }

        if (result.Error.FailureKind != FailureKind.Isolable)
            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

        // Silverback's MoveMessageErrorPolicy cannot move BatchSequence messages to retry topic.
        // Republish each original event manually so the retry subscriber processes them one by one.
        await RepublishToRetryAsync(originalMessages, cancellationToken);
    }

    private async Task RepublishToRetryAsync(
        IReadOnlyCollection<ProjectionIntegrationEvent<ProjectionTestReadModel>> messages,
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

    private static ProjectionCommandItem MapEvent(
        ProjectionIntegrationEvent<ProjectionTestReadModel> message) =>
        new(
            message.SourceAggregateId,
            message.Operation == OperationType.Deleted
                ? null
                : new ProjectionTestDto { Id = message.SourceAggregateId, Payload = message.Value.Payload },
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);
}
