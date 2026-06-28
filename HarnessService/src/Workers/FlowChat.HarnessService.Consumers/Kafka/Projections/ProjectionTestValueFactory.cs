using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.Shared.Consumers.ProjectionBulk;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionTestValueFactory
    : IProjectionValueFactory<ProjectionTestReadModel, ProjectionTestDto, Guid>
{
    public ProjectionTestDto MapValue(
        ProjectionIntegrationEvent<ProjectionTestReadModel> message) =>
        new() { Id = message.SourceAggregateId, Payload = message.Value.Payload };

    public Guid GetDeduplicationKey(ProjectionTestDto value) =>
        value.Id;
}
