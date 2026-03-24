using FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;

namespace FlowChat.UserProfileService.Api.Features.UserProfiles.Public.GetUserProfile;

public sealed record GetUserProfileResponse(UserProfileDto UserProfile);
