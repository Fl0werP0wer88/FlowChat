using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.Shared.Consumers.ProjectionBulk;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionTestCommandItemFactory
    : IProjectionCommandItemFactory<ProjectionTestReadModel, ProjectionCommandItem>
{
    public ProjectionCommandItem MapItem(
        ProjectionIntegrationEvent<ProjectionTestReadModel> message) =>
        new(
            message.SourceAggregateId,
            message.Operation == OperationType.Deleted
                ? null
                : new ProjectionTestDto { Id = message.SourceAggregateId, Payload = message.Value.Payload },
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);
}
