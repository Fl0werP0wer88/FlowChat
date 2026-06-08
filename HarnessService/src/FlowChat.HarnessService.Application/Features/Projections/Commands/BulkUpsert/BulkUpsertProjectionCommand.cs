using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;

public sealed record BulkUpsertProjectionCommand(
    IReadOnlyCollection<ProjectionCommandItem> Items)
    : IProjectionBulkCommand<ProjectionCommandItem>;

public sealed record ProjectionCommandItem(
    Guid Id,
    ProjectionTestDto? Value,
    int SourceVersion,
    DateTimeOffset SourceCreatedAtUtc,
    DateTimeOffset SourceLastModifiedAtUtc,
    DateTimeOffset? SourceDeletedAtUtc)
    : IProjectionCommandItem<ProjectionTestDto>;
