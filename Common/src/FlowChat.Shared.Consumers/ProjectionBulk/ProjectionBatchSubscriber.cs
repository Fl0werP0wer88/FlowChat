using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using MediatR;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Subscribers;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public sealed class ProjectionBatchSubscriber<TReadModel, TItem>(
    IMediator mediator,
    IPublisher publisher,
    IProjectionValueFactory<TReadModel, TItem> valueFactory,
    ILogger<ProjectionBatchSubscriber<TReadModel, TItem>> logger)
    : ProjectionBatchSubscriberBase<TReadModel, TItem>(
        mediator,
        publisher,
        valueFactory,
        logger)
    where TReadModel : class
    where TItem : class
{
    [Subscribe]
    public Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<TReadModel>> messages,
        CancellationToken cancellationToken) =>
        HandleBatchAsync(messages, cancellationToken);
}
