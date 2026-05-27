using FlowChat.ChatService.Api.Features.UserProfile.Internal.UserProfileProjection;
using FlowChat.Core.Contracts;

namespace FlowChat.ChatService.Api.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;

public sealed class BulkUpsertUserProfileProjectionRequest : IServiceInput
{
    public IReadOnlyCollection<UserProfileProjectionRequest> Items { get; init; } = [];
}
