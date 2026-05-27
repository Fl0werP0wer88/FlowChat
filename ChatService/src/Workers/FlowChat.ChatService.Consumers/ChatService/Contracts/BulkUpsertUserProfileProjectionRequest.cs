using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Consumers.ChatService.Contracts;

public sealed class BulkUpsertUserProfileProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<UserProfileProjectionRequest> Items { get; init; } = [];
}
