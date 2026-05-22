using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfileByFriendlyUserId;

public sealed record GetUserProfileByFriendlyUserIdQuery(string FriendlyUserId) : IQuery<UserProfileDto>;
