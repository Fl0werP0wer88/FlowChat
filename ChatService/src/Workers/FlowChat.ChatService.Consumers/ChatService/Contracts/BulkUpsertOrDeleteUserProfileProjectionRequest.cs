using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Consumers.ChatService.Contracts;

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequestItem
{
    public Guid UserProfileId { get; init; }
    public UserProfileProjectionRequest? Value { get; init; }
}
