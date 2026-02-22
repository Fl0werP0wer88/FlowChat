using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Enums;

namespace FlowChat.SocialGraphService.Domain.Entities;

public class Invitation : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime? RespondedAtUtc { get; set; }
}
