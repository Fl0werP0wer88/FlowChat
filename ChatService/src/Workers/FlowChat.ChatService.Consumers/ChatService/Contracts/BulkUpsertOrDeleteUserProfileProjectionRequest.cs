using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Consumers.ChatService.Contracts;

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequestItem
{
    public Guid UserProfileId { get; init; }
    public int SourceVersion { get; init; }
    public DateTimeOffset SourceCreatedAtUtc { get; init; }
    public DateTimeOffset SourceLastModifiedAtUtc { get; init; }
    public DateTimeOffset? SourceDeletedAtUtc { get; init; }
    public UserProfileProjectionRequest? Value { get; init; }
}
