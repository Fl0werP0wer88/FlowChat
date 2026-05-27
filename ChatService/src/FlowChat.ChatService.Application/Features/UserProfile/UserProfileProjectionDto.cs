namespace FlowChat.ChatService.Application.Features.UserProfile;

public sealed class UserProfileProjectionDto
{
    public Guid UserProfileId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Source { get; set; } = string.Empty;
}
