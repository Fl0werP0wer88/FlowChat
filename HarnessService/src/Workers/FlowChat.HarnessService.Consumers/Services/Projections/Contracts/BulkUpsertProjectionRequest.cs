using FlowChat.Core.Contracts;

namespace FlowChat.HarnessService.Consumers.Services.Projections.Contracts;

public sealed class BulkUpsertProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<BulkUpsertProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertProjectionRequestItem : IConsumerOutput
{
    public Guid Id { get; init; }
    public string? Payload { get; init; }
    public int SourceVersion { get; init; }
    public DateTimeOffset SourceCreatedAtUtc { get; init; }
    public DateTimeOffset SourceLastModifiedAtUtc { get; init; }
    public DateTimeOffset? SourceDeletedAtUtc { get; init; }
}
