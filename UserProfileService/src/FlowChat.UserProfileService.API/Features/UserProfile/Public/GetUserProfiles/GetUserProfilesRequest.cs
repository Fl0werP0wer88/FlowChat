using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfiles;

public sealed class GetUserProfilesRequest : IServiceInput
{
    public IReadOnlyList<Guid> UserIds { get; init; } = [];
}
