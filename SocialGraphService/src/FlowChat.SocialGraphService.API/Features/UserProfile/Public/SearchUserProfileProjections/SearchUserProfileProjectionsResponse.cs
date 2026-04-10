using FlowChat.Core.Contracts;
using FlowChat.SocialGraphService.Application.Features.UserProfile;

namespace FlowChat.SocialGraphService.Api.Features.UserProfile.Public.SearchUserProfileProjections;

public sealed record SearchUserProfileProjectionsResponse(IReadOnlyList<UserProfileProjectionDto> UserProfiles) : IServiceOutput;

