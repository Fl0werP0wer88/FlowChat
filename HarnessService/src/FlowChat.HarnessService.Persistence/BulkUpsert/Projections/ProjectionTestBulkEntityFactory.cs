using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Persistence.Entities.Projections;
using FlowChat.Shared.Persistance.ProjectionBulk;

namespace FlowChat.HarnessService.Persistence.BulkUpsert.Projections;

public sealed class ProjectionTestBulkEntityFactory
    : IProjectionBulkEntityFactory<ProjectionCommandItem, ProjectionTestDto, ProjectionTestEntity>
{
    public IReadOnlyList<string> UpdateByProperties { get; } = [nameof(ProjectionTestEntity.Id)];

    public ProjectionTestEntity CreateUpsertEntity(
        ProjectionTestDto value,
        int sourceVersion,
        DateTimeOffset sourceCreatedAtUtc,
        DateTimeOffset sourceLastModifiedAtUtc,
        DateTimeOffset? sourceDeletedAtUtc) =>
        new()
        {
            Id = value.Id,
            Payload = value.Payload,
            SourceVersion = sourceVersion,
            SourceCreatedAtUtc = sourceCreatedAtUtc,
            SourceLastModifiedAtUtc = sourceLastModifiedAtUtc,
            SourceDeletedAtUtc = sourceDeletedAtUtc
        };

    public ProjectionTestEntity CreateTombstoneEntity(
        ProjectionCommandItem item,
        DateTimeOffset now) =>
        new()
        {
            Id = item.Id,
            Payload = string.Empty,
            SourceVersion = item.SourceVersion,
            SourceCreatedAtUtc = item.SourceCreatedAtUtc,
            SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
            SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now
        };
}
