using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public sealed class ProjectionRetrySubscriber<TReadModel, TItem>(
    IMediator mediator,
    IProjectionCommandItemFactory<TReadModel, TItem> itemFactory,
    ILogger<ProjectionRetrySubscriber<TReadModel, TItem>> logger)
    : ProjectionRetrySubscriberBase<TReadModel, TItem>(
        mediator,
        itemFactory,
        logger)
    where TReadModel : class
    where TItem : notnull
{
    [Subscribe]
    public Task HandleAsync(
        ProjectionIntegrationEvent<TReadModel> message,
        CancellationToken cancellationToken) =>
        HandleRetryAsync(message, cancellationToken);
}
