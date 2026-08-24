using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfilesRangeAscending;

public sealed record SearchUserProfilesRangeAscendingQuery(
    string? FirstName,
    string? LastName,
    string? Organization,
    string? Cursor,
    int Limit) : IQuery<SearchUserProfilesRangeAscendingPageDto>;
