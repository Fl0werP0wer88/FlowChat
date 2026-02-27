namespace FlowChat.SocialGraphService.Persistence.Entities;

public class InvitationEntity
{
    public Guid Id { get; set; }
    public Guid? UserSocialGraphId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? RespondedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public UserSocialGraphEntity? UserSocialGraph { get; set; }
}
