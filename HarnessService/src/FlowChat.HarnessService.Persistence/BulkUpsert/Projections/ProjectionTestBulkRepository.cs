using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Persistence.Entities.Projections;
using FlowChat.Shared.Persistance.BulkUpsert;

namespace FlowChat.HarnessService.Persistence.BulkUpsert.Projections;

public sealed class ProjectionTestBulkRepository(AppDbContext dbContext)
    : ProjectionBulkRepositoryBase<AppDbContext, ProjectionCommandItem, ProjectionTestDto, ProjectionTestEntity>(dbContext),
        IProjectionTestBulkRepository
{
    public Task BulkUpsertOrSoftDeleteAsync(
        IReadOnlyCollection<ProjectionCommandItem> items,
        CancellationToken cancellationToken) =>
        BulkUpsertProjectionAsync(
            items,
            [nameof(ProjectionTestEntity.Id)],
            cancellationToken);

    protected override ProjectionTestEntity CreateUpsertEntity(
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

    protected override ProjectionTestEntity CreateTombstoneEntity(
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
