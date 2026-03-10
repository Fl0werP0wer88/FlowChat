namespace FlowChat.Messaging.Contracts.UserProfileService.Events;

public sealed class UserProfileCreatedIntegrationEvent : IntegrationEvent
{
    public Guid UserProfileId { get; init; }
    public required string UserName { get; init; }
    public required string DisplayName { get; init; }
    public string? MainEmail { get; init; }
    public string? MainPhone { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastSeenAtUtc { get; init; }
    public bool IsEmailVisible { get; init; }
    public bool IsPhoneVisible { get; init; }
}
