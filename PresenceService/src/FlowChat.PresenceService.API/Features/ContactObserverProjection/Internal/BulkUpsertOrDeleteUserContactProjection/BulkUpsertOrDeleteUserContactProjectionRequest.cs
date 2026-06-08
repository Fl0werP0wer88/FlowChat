using FlowChat.Core.Contracts;
namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.BulkUpsertOrDeleteUserContactProjection;

public sealed class BulkUpsertOrDeleteUserContactProjectionRequest : IServiceInput
{
    public IReadOnlyCollection<BulkUpsertOrDeleteUserContactProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertOrDeleteUserContactProjectionRequestItem : IServiceInput
{
    public Guid ObservedUserId { get; init; }
    public Guid ObserverUserId { get; init; }
    public int SourceVersion { get; init; }
    public DateTimeOffset SourceCreatedAtUtc { get; init; }
    public DateTimeOffset SourceLastModifiedAtUtc { get; init; }
    public DateTimeOffset? SourceDeletedAtUtc { get; init; }
    public BulkUpsertOrDeleteUserContactProjectionRequestValue? Value { get; init; }
}

public sealed class BulkUpsertOrDeleteUserContactProjectionRequestValue : IServiceInput
{
    public string Source { get; init; } = string.Empty;
}
