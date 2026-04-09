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
    public bool IsVisible { get; private set; }

    private Email(
        Id<Email>? id,
        Id<UserProfile> userProfileId,
        EmailAddress address,
        bool isMain = false,
        bool isAuth = false,
        bool isConfirmed = false,
        bool isVisible = true) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(address);

        UserProfileId = userProfileId;
        Address = address;
        IsMain = isMain;
        IsAuth = isAuth;
        IsConfirmed = isConfirmed;
        IsVisible = isVisible;
    }

    public static Email Create(
        Id<UserProfile> userProfileId,
        EmailAddress address,
        bool isMain = false,
        bool isAuth = false,
        Id<Email>? id = null)
    {
        return new Email(id, userProfileId, address, isMain, isAuth, isConfirmed: false, isVisible: true);
    }

    internal void Confirm()
    {
        IsConfirmed = true;
    }

    internal void SetMain(bool isMain)
    {
        if (isMain && !IsConfirmed)
        {
            throw new InvalidOperationException($"Email '{Address.Value}' must be confirmed before it can be set as main.");
        }

        IsMain = isMain;
    }

    internal void SetVisible(bool isVisible)
    {
        IsVisible = isVisible;
    }

    internal void SetAuth(bool isAuth)
    {
        if (isAuth && !IsConfirmed)
        {
            throw new InvalidOperationException($"Email '{Address.Value}' must be confirmed before it can be set as auth.");
        }

        IsAuth = isAuth;
    }
}
