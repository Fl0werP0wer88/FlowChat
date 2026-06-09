using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.HarnessService.Consumers.Services;
using FlowChat.HarnessService.Consumers.Services.Projections.Contracts;
using Silverback.Messaging.Subscribers;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionBatchSubscriber(
    IHarnessApiClient harnessApiClient,
    ILogger<ProjectionBatchSubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ProjectionMainConsumerName)]
    public async Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<ProjectionTestReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<BulkUpsertProjectionRequestItem>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            if (message.SourceAggregateVersion <= 0)
                throw new NonTransientException("Payload does not contain valid SourceVersion.");

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

            logger.LogInformation(
                "Processed {Count} projection events from Kafka batch.",
                items.Count);
        }
        catch (IsolableException)
        {
            // Silverback's MoveMessageErrorPolicy cannot route batch-sequence messages to the retry
            // topic — it warns and discards them. Fall back to per-item processing so that valid
            // items are still projected and only the isolable item is dropped.
            await ProcessItemsIndividuallyAsync(items, cancellationToken);
        }
    }

    private async Task ProcessItemsIndividuallyAsync(
        IReadOnlyCollection<BulkUpsertProjectionRequestItem> items,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Batch upsert failed with IsolableException; retrying {Count} items individually.",
            items.Count);

        foreach (var item in items)
        {
            try
            {
                await harnessApiClient.BulkUpsertProjectionAsync(
                    new BulkUpsertProjectionRequest { Items = [item] },
                    cancellationToken);
            }
            catch (IsolableException ex)
            {
                logger.LogError(
                    ex,
                    "Projection item {Id} failed per-item retry and will be skipped.",
                    item.Id);
            }
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
