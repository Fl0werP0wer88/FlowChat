using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.Shared.Consumers.ProjectionBulk;
using MediatR;
using Silverback.Messaging.Subscribers;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionRetrySubscriber(
    IMediator mediator,
    IProjectionCommandItemFactory<ProjectionTestReadModel, ProjectionCommandItem> itemFactory,
    ILogger<ProjectionRetrySubscriber> logger)
    : ProjectionRetrySubscriberBase<ProjectionTestReadModel, ProjectionCommandItem>(
        mediator,
        itemFactory,
        logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ProjectionRetryConsumerName)]
    public Task HandleAsync(
        ProjectionIntegrationEvent<ProjectionTestReadModel> message,
        CancellationToken cancellationToken) =>
        HandleRetryAsync(message, cancellationToken);
}
