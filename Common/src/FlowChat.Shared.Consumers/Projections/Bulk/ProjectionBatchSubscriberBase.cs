using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.Shared.Consumers.Projections.Bulk;

public abstract class ProjectionBatchSubscriberBase<TReadModel, TItem, TKey>(
    IMediator mediator,
    IProjectionValueFactory<TReadModel, TItem, TKey> valueFactory,
    ILogger logger)
    where TReadModel : class
    where TItem : class
    where TKey : notnull
{
    protected async Task HandleBatchAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<TReadModel>> messages,
        CancellationToken cancellationToken)
    {
        var items = new List<ProjectionCommandItem<TItem>>();

        await foreach (var message in messages.WithCancellation(cancellationToken))
        {
            ValidateMessage(message);

            items.AddRange(MapItems(message));
        }

        if (items.Count == 0)
        {
            logger.LogDebug("Skipping empty projection batch.");
            return;
        }

        var deduplicatedItems = Deduplicate(items);
        var result = await mediator.Send(new ProjectionBulkCommand<ProjectionCommandItem<TItem>>(deduplicatedItems), cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Processed {Count} projection events from Kafka batch.", deduplicatedItems.Count);
            return;
        }

        throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");
    }

    private static void ValidateMessage(ProjectionIntegrationEvent<TReadModel> message)
    {
        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");
    }

    private IReadOnlyCollection<ProjectionCommandItem<TItem>> Deduplicate(
        IReadOnlyCollection<ProjectionCommandItem<TItem>> items) =>
        items
            .Select((item, index) => new { item, index })
            .GroupBy(x => valueFactory.GetDeduplicationKey(x.item.Value))
            .Select(group => group
                .OrderBy(x => x.item.SourceVersion)
                .ThenBy(x => x.index)
                .Last())
            .OrderBy(x => x.index)
            .Select(x => x.item)
            .ToArray();

    private IEnumerable<ProjectionCommandItem<TItem>> MapItems(ProjectionIntegrationEvent<TReadModel> message) =>
        valueFactory.MapValues(message).Select(value => new ProjectionCommandItem<TItem>(
            value,
            message.Operation,
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt));
}
