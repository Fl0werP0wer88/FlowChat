using FlowChat.Domain.Abstractions;

namespace FlowChat.UserProfileService.Domain.Entities;

public class Email : EntityBase<Email>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public string Address { get; private set; }
    public bool IsMain { get; private set; }

    private Email(
        Id<Email>? id,
        Id<UserProfile> userProfileId,
        string address,
        bool isMain = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        UserProfileId = userProfileId;
        Address = address.Trim();
        IsMain = isMain;
    }

    public static Email Create(
        Id<UserProfile> userProfileId,
        string address,
        bool isMain = false,
        Id<Email>? id = null)
    {
        return new Email(id, userProfileId, address, isMain);
    }

    public static Email Rehydrate(
        Id<UserProfile> userProfileId,
        string address,
        bool isMain = false,
        Id<Email>? id = null)
    {
        return new Email(id, userProfileId, address, isMain);
    }

    internal void SetMain(bool isMain)
    {
        IsMain = isMain;
    }
}
