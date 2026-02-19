using FlowChat.UserProfileService.Domain.Common;
using FlowChat.UserProfileService.Domain.Enums;

namespace FlowChat.UserProfileService.Domain.Entities;

public class Contact : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public ContactStatus Status { get; set; } = ContactStatus.Pending;
    public DateTime? RespondedAtUtc { get; set; }
    public UserProfile Requester { get; set; } = null!;
    public UserProfile Addressee { get; set; } = null!;
}
