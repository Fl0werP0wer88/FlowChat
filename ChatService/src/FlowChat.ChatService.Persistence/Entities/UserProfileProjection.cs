namespace FlowChat.ChatService.Persistence.Entities;

public sealed class UserProfileProjection
{
    public Guid UserId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
