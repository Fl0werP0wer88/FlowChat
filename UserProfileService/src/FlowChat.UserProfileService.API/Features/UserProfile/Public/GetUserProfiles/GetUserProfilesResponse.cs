using FlowChat.Core.Contracts;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.GetUserProfiles;

public sealed record GetUserProfilesResponse(IReadOnlyList<UserProfileDto> UserProfiles) : IServiceOutput;
