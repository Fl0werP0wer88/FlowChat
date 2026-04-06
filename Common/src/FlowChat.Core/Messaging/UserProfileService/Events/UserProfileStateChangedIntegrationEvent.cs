namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed class UserProfileStateChangedIntegrationEvent : IntegrationEvent
{
    public Guid UserProfileId { get; init; }
    public required string FriendlyUserId { get; init; }
    public required string DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
    public string? MainEmail { get; init; }
    public bool? IsMainEmailConfirmed { get; init; }
    public string? MainPhone { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LastSeenAtUtc { get; init; }
    public bool IsEmailVisible { get; init; }
    public bool IsPhoneVisible { get; init; }
}
