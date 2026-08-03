using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.Shared.Consumers.Projections.Bulk;

public sealed class ProjectionBatchSubscriber<TReadModel, TItem, TKey>(
    IMediator mediator,
    IPublisher publisher,
    IProjectionValueFactory<TReadModel, TItem, TKey> valueFactory,
    ILogger<ProjectionBatchSubscriber<TReadModel, TItem, TKey>> logger)
    : ProjectionBatchSubscriberBase<TReadModel, TItem, TKey>(
        mediator,
        publisher,
        valueFactory,
        logger)
    where TReadModel : class
    where TItem : class
    where TKey : notnull
{
    [Subscribe]
    public Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<TReadModel>> messages,
        CancellationToken cancellationToken) =>
        HandleBatchAsync(messages, cancellationToken);
}
