using FlowChat.SocialGraphService.Domain.Common;

namespace FlowChat.SocialGraphService.Domain.Entities;

public class Contact : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId1 { get; set; }
    public Guid UserId2 { get; set; }
    public bool IsBlocked { get; set; } = false;
    public Guid? BlockedBy { get; set; }
}
