using FlowChat.Core.Contracts;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfile;

public sealed record GetUserProfileResponse(UserProfileDto UserProfile) : IServiceOutput;
