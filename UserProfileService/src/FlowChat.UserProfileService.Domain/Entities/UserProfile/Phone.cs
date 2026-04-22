using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public class Phone : EntityBase<Phone>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public PhoneNumber Number { get; private set; }
    public bool IsMain { get; private set; }
    public bool IsConfirmed { get; private set; }
    public bool IsVisible { get; private set; }

    private Phone(
        Id<Phone> id,
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        bool isConfirmed = false,
        bool isVisible = true) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(number);

        UserProfileId = userProfileId;
        Number = number;
        IsMain = isMain;
        IsConfirmed = isConfirmed;
        IsVisible = isVisible;
    }

    public static Phone Create(
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        bool isConfirmed = false,
        Id<Phone>? id = null,
        bool isVisible = true)
    {
        return new Phone(id ?? Id<Phone>.New(), userProfileId, number, isMain, isConfirmed, isVisible);
    }

    public static Phone Rehydrate(
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        bool isConfirmed = false,
        Id<Phone>? id = null,
        bool isVisible = true)
    {
        return new Phone(id ?? Id<Phone>.New(), userProfileId, number, isMain, isConfirmed, isVisible);
    }

    internal void Confirm()
    {
        IsConfirmed = true;
    }

    internal void SetMain(bool isMain)
    {
        if (isMain && !IsConfirmed)
        {
            throw new InvalidOperationException($"Phone '{Number.Value}' must be confirmed before it can be set as main.");
        }

        IsMain = isMain;
    }

    internal void SetVisible(bool isVisible)
    {
        IsVisible = isVisible;
    }
}

