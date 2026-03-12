using FlowChat.Application.Abstractions;

namespace FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto>;
