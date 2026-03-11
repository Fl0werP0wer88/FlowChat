namespace FlowChat.SocialGraphService.Persistence.Entities;

public sealed class UserProfileReadModelEntity
{
    public Guid UserProfileId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? MainEmail { get; set; }
    public string? MainPhone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public bool IsEmailVisible { get; set; }
    public bool IsPhoneVisible { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
}
