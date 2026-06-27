using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public sealed class ProjectionRetrySubscriber<TReadModel, TItem>(
    IMediator mediator,
    IProjectionValueFactory<TReadModel, TItem> valueFactory,
    ILogger<ProjectionRetrySubscriber<TReadModel, TItem>> logger)
    : ProjectionRetrySubscriberBase<TReadModel, TItem>(
        mediator,
        valueFactory,
        logger)
    where TReadModel : class
    where TItem : class
{
    [Subscribe]
    public Task HandleAsync(
        ProjectionIntegrationEvent<TReadModel> message,
        CancellationToken cancellationToken) =>
        HandleRetryAsync(message, cancellationToken);
}
