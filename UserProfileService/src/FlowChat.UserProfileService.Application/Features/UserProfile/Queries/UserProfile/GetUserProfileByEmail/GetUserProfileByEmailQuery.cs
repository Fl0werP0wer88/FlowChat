using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfileByEmail;

public sealed record GetUserProfileByEmailQuery(string Email) : IQuery<UserProfileDto>;
