using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public class Email : EntityBase<Email>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public EmailAddress Address { get; private set; }
    public bool IsMain { get; private set; }
    public bool IsAuth { get; private set; }
    public bool IsConfirmed { get; private set; }

    private Email(
        Id<Email>? id,
        Id<UserProfile> userProfileId,
        EmailAddress address,
        bool isMain = false,
        bool isAuth = false,
        bool isConfirmed = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(address);

        UserProfileId = userProfileId;
        Address = address;
        IsMain = isMain;
        IsAuth = isAuth;
        IsConfirmed = isConfirmed;
    }

    public static Email Create(
        Id<UserProfile> userProfileId,
        EmailAddress address,
        bool isMain = false,
        bool isAuth = false,
        Id<Email>? id = null)
    {
        return new Email(id, userProfileId, address, isMain, isAuth, isConfirmed: false);
    }

    internal void Confirm()
    {
        IsConfirmed = true;
    }

    internal void SetMain(bool isMain)
    {
        IsMain = isMain;
    }

    internal void SetAuth(bool isAuth)
    {
        IsAuth = isAuth;
    }
}

