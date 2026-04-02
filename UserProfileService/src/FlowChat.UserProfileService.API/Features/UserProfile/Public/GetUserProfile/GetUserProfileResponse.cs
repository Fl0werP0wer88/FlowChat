using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfile;

public sealed record GetUserProfileResponse(UserProfileDto UserProfile);
