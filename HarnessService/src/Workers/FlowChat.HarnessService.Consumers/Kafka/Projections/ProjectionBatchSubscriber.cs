using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.HarnessService.Consumers.Services;
using FlowChat.HarnessService.Consumers.Services.Projections.Contracts;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionBatchSubscriber(
    IHarnessApiClient harnessApiClient,
    IPublisher publisher,
    ILogger<ProjectionBatchSubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ProjectionMainConsumerName)]
    public async Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<ProjectionTestReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<BulkUpsertProjectionRequestItem>();
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

        try
        {
            await harnessApiClient.BulkUpsertProjectionAsync(
                new BulkUpsertProjectionRequest { Items = items },
                cancellationToken);

            logger.LogInformation("Processed {Count} projection events from Kafka batch.", items.Count);
        }
        catch (IsolableException)
        {
            // Silverback's MoveMessageErrorPolicy cannot move BatchSequence messages to retry topic.
            // Republish each original event manually so the retry subscriber processes them one by one.
            await RepublishToRetryAsync(originalMessages, cancellationToken);
        }
    }

    private async Task RepublishToRetryAsync(
        IReadOnlyCollection<ProjectionIntegrationEvent<ProjectionTestReadModel>> messages,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Batch upsert failed with IsolableException; republishing {Count} events to retry topic.",
            messages.Count);

        foreach (var message in messages)
        {
            await publisher.PublishAsync(message, cancellationToken);
        }
    }

    private static BulkUpsertProjectionRequestItem MapEvent(
        ProjectionIntegrationEvent<ProjectionTestReadModel> message) =>
        new()
        {
            Id = message.SourceAggregateId,
            Payload = message.Operation == OperationType.Deleted ? null : message.Value.Payload,
            SourceVersion = message.SourceAggregateVersion,
            SourceCreatedAtUtc = message.SourceAggregateCreatedAtUtc,
            SourceLastModifiedAtUtc = message.SourceAggregateModifiedAtUtc,
            SourceDeletedAtUtc = message.SourceAggregateDeletedAt
        };
}
