using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities;

public class Email : EntityBase<Email>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public EmailAddress Address { get; private set; }
    public bool IsMain { get; private set; }

    private Email(
        Id<Email>? id,
        Id<UserProfile> userProfileId,
        EmailAddress address,
        bool isMain = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(address);

        UserProfileId = userProfileId;
        Address = address;
        IsMain = isMain;
    }

    public static Email Create(
        Id<UserProfile> userProfileId,
        EmailAddress address,
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

