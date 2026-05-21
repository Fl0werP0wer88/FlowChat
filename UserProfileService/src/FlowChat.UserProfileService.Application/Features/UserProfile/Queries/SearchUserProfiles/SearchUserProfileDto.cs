using FlowChat.Core.Contracts;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Queries.SearchUserProfiles;

public sealed class SearchUserProfileDto : IDbReadResponse
{
    public Guid UserProfileId { get; init; }
    public required string FriendlyUserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public SearchUserProfileEmailDto? MainEmail { get; init; }
    public SearchUserProfilePhoneDto? MainPhone { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
}
