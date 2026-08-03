using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;

namespace FlowChat.Shared.Consumers.Projections.Bulk;

public sealed class ProjectionRetrySubscriber<TReadModel, TItem, TKey>(
    IMediator mediator,
    IProjectionValueFactory<TReadModel, TItem, TKey> valueFactory,
    ILogger<ProjectionRetrySubscriber<TReadModel, TItem, TKey>> logger)
    : ProjectionRetrySubscriberBase<TReadModel, TItem, TKey>(
        mediator,
        valueFactory,
        logger)
    where TReadModel : class
    where TItem : class
    where TKey : notnull
{
    [Subscribe]
    public Task HandleAsync(
        ProjectionIntegrationEvent<TReadModel> message,
        CancellationToken cancellationToken) =>
        HandleRetryAsync(message, cancellationToken);
}
