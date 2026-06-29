using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfiles;

public sealed record GetUserProfilesQuery(IReadOnlyList<Guid> UserIds) : IQuery<IReadOnlyList<UserProfileDto>>;
