namespace FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;

public sealed class UpsertUserProfileReadModelRequest
{
    public Guid UserProfileId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? MainEmail { get; set; }
    public string? MainPhone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public bool IsEmailVisible { get; set; }
    public bool IsPhoneVisible { get; set; }
}
