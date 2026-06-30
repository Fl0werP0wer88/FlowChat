using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile;

public sealed class UserProfileProjectionDto : IDbReadResponse
{
    public Guid UserProfileId { get; init; }
    public required string FriendlyUserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public UserProfileProjectionEmailDto? MainEmail { get; init; }
    public UserProfileProjectionPhoneDto? MainPhone { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
    public int SourceVersion { get; init; }
    public string Source { get; init; } = string.Empty;
}
