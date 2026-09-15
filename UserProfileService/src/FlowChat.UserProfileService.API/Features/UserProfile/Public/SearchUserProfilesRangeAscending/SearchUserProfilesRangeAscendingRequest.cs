using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfilesRangeAscending;

public sealed class SearchUserProfilesRangeAscendingRequest : IServiceInput
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public string? Cursor { get; init; }
    public int Limit { get; init; } = SearchUserProfilesRangeAscendingController.DefaultLimit;
}
