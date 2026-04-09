namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed class UserProfileChangedIntegrationEvent : IntegrationEvent
{
    public Guid UserProfileId { get; init; }
    public required string FriendlyUserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public UserProfileEmail? MainEmail { get; init; }
    public UserProfilePhone? MainPhone { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
}
