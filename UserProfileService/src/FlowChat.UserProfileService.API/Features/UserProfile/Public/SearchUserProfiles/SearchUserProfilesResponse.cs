using FlowChat.Core.Contracts;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfiles;

namespace FlowChat.UserProfileService.Api.Features.UserProfile.Public.SearchUserProfiles;

public sealed record SearchUserProfilesResponse(IReadOnlyList<SearchUserProfileDto> UserProfiles) : IServiceOutput;
