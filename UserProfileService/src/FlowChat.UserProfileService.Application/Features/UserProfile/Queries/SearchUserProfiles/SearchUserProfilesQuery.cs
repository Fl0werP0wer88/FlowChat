using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.SearchUserProfiles;

public sealed record SearchUserProfilesQuery(
    string? FirstName,
    string? LastName,
    string? Organization) : IQuery<IReadOnlyList<SearchUserProfileDto>>;
