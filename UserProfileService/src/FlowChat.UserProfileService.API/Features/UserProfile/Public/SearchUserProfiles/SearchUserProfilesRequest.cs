using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfiles;

public sealed class SearchUserProfilesRequest : IServiceInput
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
}
