using FlowChat.Core.Contracts;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile;

public sealed class UserProfileProjection : IDbResponse
{
    public Guid UserProfileId { get; init; }
    public required string FriendlyUserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public UserProfileProjectionEmail? MainEmail { get; init; }
    public UserProfileProjectionPhone? MainPhone { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
}
