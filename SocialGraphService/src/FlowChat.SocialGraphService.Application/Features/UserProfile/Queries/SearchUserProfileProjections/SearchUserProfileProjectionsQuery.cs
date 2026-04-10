using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Queries.SearchUserProfileProjections;

public sealed record SearchUserProfileProjectionsQuery(
    string? FirstName,
    string? LastName,
    string? Organization) : IQuery<IReadOnlyList<UserProfileProjectionDto>>;

