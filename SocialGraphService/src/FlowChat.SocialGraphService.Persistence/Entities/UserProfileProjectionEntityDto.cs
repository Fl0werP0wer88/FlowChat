namespace FlowChat.SocialGraphService.Persistence.Entities;

public sealed class UserProfileProjectionEntityDto
{
    public Guid UserProfileId { get; set; }
    public string FriendlyUserId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
    public string? MainEmail { get; set; }
    public bool? MainEmailIsConfirmed { get; set; }
    public bool? MainEmailIsVisible { get; set; }
    public string? MainPhone { get; set; }
    public bool? MainPhoneIsConfirmed { get; set; }
    public bool? MainPhoneIsVisible { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
}
