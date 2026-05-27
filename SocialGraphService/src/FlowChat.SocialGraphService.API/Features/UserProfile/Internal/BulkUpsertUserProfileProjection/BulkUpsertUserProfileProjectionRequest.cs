using FlowChat.Core.Contracts;
using FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.UserProfileProjection;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Internal.BulkUpsertUserProfileProjection;

public sealed class BulkUpsertUserProfileProjectionRequest : IServiceInput
{
    public IReadOnlyCollection<UserProfileProjectionRequest> Items { get; init; } = [];
}
