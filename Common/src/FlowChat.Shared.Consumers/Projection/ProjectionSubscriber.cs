using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.Shared.Consumers.Projection;

public sealed class ProjectionSubscriber<TReadModel, TValue>(
    IMediator mediator,
    IProjectionValueFactory<TReadModel, TValue> valueFactory,
    ILogger<ProjectionSubscriber<TReadModel, TValue>> logger)
    : SubscriberBase<ProjectionIntegrationEvent<TReadModel>>(logger)
    where TReadModel : class
    where TValue : class
{
    protected override async Task ExecuteAsync(
        ProjectionIntegrationEvent<TReadModel> message,
        CancellationToken cancellationToken)
    {
        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");

        var item = new ProjectionCommandItem<TValue>(
            valueFactory.MapValue(message),
            message.Operation,
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);

        var result = await mediator.Send(new ProjectionCommand<TValue>(item), cancellationToken);
        ThrowIfFailure(result);
    }
}
