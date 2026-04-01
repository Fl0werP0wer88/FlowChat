using FlowChat.Shared.Application;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto>;

