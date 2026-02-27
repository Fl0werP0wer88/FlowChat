namespace FlowChat.SocialGraphService.Persistence.Entities;

public class UserSocialGraphEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ICollection<ContactEntity> Contacts { get; set; } = new List<ContactEntity>();
    public ICollection<InvitationEntity> Invitations { get; set; } = new List<InvitationEntity>();
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
}
