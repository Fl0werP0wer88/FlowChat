using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfilesRangeAscending;

public sealed record SearchUserProfilesRangeAscendingResponse(
    IReadOnlyList<UserProfileSearchResultResponse> Items,
    string? NextCursor,
    bool HasMore) : IServiceOutput;

public sealed record UserProfileSearchResultResponse(
    Guid Id,
    string FriendlyUserId,
    string? FirstName,
    string? LastName,
    string? Organization,
    string? AvatarUrl);
