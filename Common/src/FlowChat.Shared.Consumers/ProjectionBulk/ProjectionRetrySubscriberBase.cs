using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public abstract class ProjectionRetrySubscriberBase<TReadModel, TItem, TKey>(
    IMediator mediator,
    IProjectionValueFactory<TReadModel, TItem, TKey> valueFactory,
    ILogger logger)
    where TReadModel : class
    where TItem : class
    where TKey : notnull
{
    protected async Task HandleRetryAsync(
        ProjectionIntegrationEvent<TReadModel> message,
        CancellationToken cancellationToken)
    {
        ValidateMessage(message);

        var item = MapItem(message);
        var result = await mediator.Send(new ProjectionBulkCommand<ProjectionCommandItem<TItem>>([item]), cancellationToken);

        if (!result.IsSuccess)
        {
            // Retry endpoints move isolable failures to DLQ based on this exception type
            if (result.Error.FailureKind == FailureKind.Isolable)
                throw new IsolableException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");
        }

        logger.LogInformation(
            "Processed projection event {Id} from Kafka retry topic.",
            message.SourceAggregateId);
    }

    private static void ValidateMessage(ProjectionIntegrationEvent<TReadModel> message)
    {
        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");
    }

    private ProjectionCommandItem<TItem> MapItem(ProjectionIntegrationEvent<TReadModel> message) =>
        new(
            valueFactory.MapValue(message),
            message.Operation,
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);
}
