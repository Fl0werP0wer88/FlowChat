using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

public sealed class BulkUpsertUserProfileProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<UserProfileProjectionRequest> Items { get; init; } = [];
}
