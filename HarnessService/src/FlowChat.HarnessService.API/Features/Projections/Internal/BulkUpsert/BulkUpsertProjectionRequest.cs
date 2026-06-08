using FlowChat.Core.Contracts;

namespace FlowChat.HarnessService.API.Features.Projections.Internal.BulkUpsert;

public sealed class BulkUpsertProjectionRequest : IServiceInput
{
    public IReadOnlyCollection<BulkUpsertProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertProjectionRequestItem : IServiceInput
{
    public Guid Id { get; init; }
    public string? Payload { get; init; }
    public int SourceVersion { get; init; }
    public DateTimeOffset SourceCreatedAtUtc { get; init; }
    public DateTimeOffset SourceLastModifiedAtUtc { get; init; }
    public DateTimeOffset? SourceDeletedAtUtc { get; init; }
}
