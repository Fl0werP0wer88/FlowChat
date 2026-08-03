using System.Diagnostics.CodeAnalysis;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.Shared.Consumers.Projections.Single;

public abstract class ProjectionSingleSubscriberBase<TReadModel, TValue>(
    IMediator mediator,
    IConsumedOffsetCommitter consumedOffsetCommitter,
    ILogger logger)
    : SubscriberBase<ProjectionIntegrationEvent<TReadModel>>(logger)
    where TReadModel : class
    where TValue : class
{
    protected sealed override async Task ExecuteAsync(
        ProjectionIntegrationEvent<TReadModel> message,
        CancellationToken cancellationToken)
    {
        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");

        if (!TryMapValue(message, out var value))
        {
            await consumedOffsetCommitter.CommitConsumedOffsetsAsync(cancellationToken);
            return;
        }

        var item = new ProjectionCommandItem<TValue>(
            value,
            message.Operation,
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);

        var result = await mediator.Send(new ProjectionSingleCommand<TValue>(item), cancellationToken);
        ThrowIfFailure(result);
    }

    protected abstract bool TryMapValue(
        ProjectionIntegrationEvent<TReadModel> message,
        [NotNullWhen(true)] out TValue? value);
}
