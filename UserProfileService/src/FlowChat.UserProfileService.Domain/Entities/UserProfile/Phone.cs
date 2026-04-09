using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public class Phone : EntityBase<Phone>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public PhoneNumber Number { get; private set; }
    public bool IsMain { get; private set; }
    public bool IsVisible { get; private set; }

    private Phone(
        Id<Phone>? id,
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        bool isVisible = true) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(number);

        UserProfileId = userProfileId;
        Number = number;
        IsMain = isMain;
        IsVisible = isVisible;
    }

    public static Phone Create(
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        Id<Phone>? id = null,
        bool isVisible = true)
    {
        return new Phone(id, userProfileId, number, isMain, isVisible);
    }

    public static Phone Rehydrate(
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        Id<Phone>? id = null,
        bool isVisible = true)
    {
        return new Phone(id, userProfileId, number, isMain, isVisible);
    }

    internal void SetMain(bool isMain)
    {
        IsMain = isMain;
    }

    internal void SetVisible(bool isVisible)
    {
        IsVisible = isVisible;
    }
}

