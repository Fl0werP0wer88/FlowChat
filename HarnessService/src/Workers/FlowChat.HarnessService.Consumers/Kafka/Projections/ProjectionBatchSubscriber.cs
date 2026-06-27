using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.Shared.Consumers.ProjectionBulk;
using MediatR;
using Silverback.Messaging.Subscribers;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionBatchSubscriber(
    IMediator mediator,
    IPublisher publisher,
    IProjectionCommandItemFactory<ProjectionTestReadModel, ProjectionCommandItem> itemFactory,
    ILogger<ProjectionBatchSubscriber> logger)
    : ProjectionBatchSubscriberBase<ProjectionTestReadModel, ProjectionCommandItem>(
        mediator,
        publisher,
        itemFactory,
        logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ProjectionMainConsumerName)]
    public Task HandleAsync(
        IAsyncEnumerable<ProjectionIntegrationEvent<ProjectionTestReadModel>> messages,
        CancellationToken cancellationToken) =>
        HandleBatchAsync(messages, cancellationToken);
}
