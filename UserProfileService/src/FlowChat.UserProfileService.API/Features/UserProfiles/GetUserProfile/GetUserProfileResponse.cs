using FlowChat.UserProfileService.Application.UserProfiles.Queries.GetUserProfile;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.GetUserProfile;

public sealed record GetUserProfileResponse(UserProfileDto UserProfile);
