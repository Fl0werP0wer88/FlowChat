using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Consumers.Presence.Contracts;

public sealed class BulkUpsertOrDeleteUserContactProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<BulkUpsertOrDeleteUserContactProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertOrDeleteUserContactProjectionRequestItem : IConsumerOutput
{
    public Guid ObservedUserId { get; init; }
    public Guid ObserverUserId { get; init; }
    public int SourceVersion { get; init; }
    public UserContactProjectionRequest? Value { get; init; }
}
