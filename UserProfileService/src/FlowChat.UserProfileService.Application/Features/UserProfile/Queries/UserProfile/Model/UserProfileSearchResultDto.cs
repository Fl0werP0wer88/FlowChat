using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

public sealed record UserProfileSearchResultDto(
    Guid Id,
    string FriendlyUserId,
    string? FirstName,
    string? LastName,
    string? Organization,
    string? AvatarUrl) : IDbReadResponse;
