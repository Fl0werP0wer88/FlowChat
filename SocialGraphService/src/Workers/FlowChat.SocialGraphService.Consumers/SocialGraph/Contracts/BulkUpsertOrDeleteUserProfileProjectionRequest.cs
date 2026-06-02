using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<BulkUpsertOrDeleteUserProfileProjectionRequestItem> Items { get; init; } = [];
}

public sealed class BulkUpsertOrDeleteUserProfileProjectionRequestItem
{
    public Guid UserProfileId { get; init; }
    public int SourceVersion { get; init; }
    public UserProfileProjectionRequest? Value { get; init; }
}
