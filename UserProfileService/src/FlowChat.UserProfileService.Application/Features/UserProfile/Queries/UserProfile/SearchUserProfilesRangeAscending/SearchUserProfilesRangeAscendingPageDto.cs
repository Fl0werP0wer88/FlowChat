using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;

public sealed record SearchUserProfilesRangeAscendingPageDto(
    IReadOnlyList<UserProfileSearchResultDto> Items,
    string? NextCursor,
    bool HasMore);
