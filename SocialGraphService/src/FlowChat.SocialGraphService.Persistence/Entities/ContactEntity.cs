namespace FlowChat.SocialGraphService.Persistence.Entities;

public class ContactEntity
{
    public Guid Id { get; set; }
    public Guid? UserSocialGraphId { get; set; }
    public Guid UserId1 { get; set; }
    public Guid UserId2 { get; set; }
    public bool IsBlocked { get; set; }
    public Guid? BlockedBy { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public UserSocialGraphEntity? UserSocialGraph { get; set; }

    public static ContactEntity Create(
        Guid id,
        Guid userId1,
        Guid userId2,
        bool isBlocked = false,
        Guid? blockedBy = null,
        Guid? userSocialGraphId = null,
        string createdBy = "",
        DateTimeOffset? createdAtUtc = null,
        string lastModifiedBy = "",
        DateTimeOffset? lastModifiedAtUtc = null)
    {
        return new ContactEntity
        {
            Id = id,
            UserSocialGraphId = userSocialGraphId,
            UserId1 = userId1,
            UserId2 = userId2,
            IsBlocked = isBlocked,
            BlockedBy = blockedBy,
            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
            LastModifiedBy = lastModifiedBy,
            LastModifiedAtUtc = lastModifiedAtUtc ?? DateTimeOffset.UtcNow
        };
    }
}
