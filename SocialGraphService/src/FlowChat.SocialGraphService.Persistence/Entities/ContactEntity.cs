namespace FlowChat.SocialGraphService.Persistence.Entities;

public class ContactEntity
{
    public Guid Id { get; set; }
    public Guid? UserSocialGraphId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid ContactUserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsBlocked { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string LastModifiedBy { get; set; } = string.Empty;
    public DateTimeOffset LastModifiedAtUtc { get; set; }
    public UserSocialGraphEntity? UserSocialGraph { get; set; }

    public static ContactEntity Create(
        Guid id,
        Guid ownerUserId,
        Guid contactUserId,
        string login,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        string? email = null,
        bool isBlocked = false,
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
            OwnerUserId = ownerUserId,
            ContactUserId = contactUserId,
            Login = login,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            Email = email,
            IsBlocked = isBlocked,
            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
            LastModifiedBy = lastModifiedBy,
            LastModifiedAtUtc = lastModifiedAtUtc ?? DateTimeOffset.UtcNow
        };
    }
}
