using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<UserProfileDto>;

