using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities;

public class Email : EntityBase<Email>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public EmailAddress Address { get; private set; }
    public bool IsMain { get; private set; }
    public bool IsConfirmed { get; private set; }

    private Email(
        Id<Email>? id,
        Id<UserProfile> userProfileId,
        EmailAddress address,
        bool isMain = false,
        bool isConfirmed = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(address);

        UserProfileId = userProfileId;
        Address = address;
        IsMain = isMain;
        IsConfirmed = isConfirmed;
    }

    public static Email Create(
        Id<UserProfile> userProfileId,
        EmailAddress address,
        bool isMain = false,
        Id<Email>? id = null)
    {
        return new Email(id, userProfileId, address, isMain, isConfirmed: false);
    }

    internal void Confirm()
    {
        IsConfirmed = true;
    }

    internal void SetMain(bool isMain)
    {
        IsMain = isMain;
    }
}

