using FlowChat.UserProfileService.Domain.Common;

namespace FlowChat.UserProfileService.Domain.Entities;

public class UserProfile : AuditableEntity
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAtUtc { get; set; }
}
