using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public class Phone : EntityBase<Phone>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public PhoneNumber Number { get; private set; }
    public bool IsMain { get; private set; }

    private Phone(
        Id<Phone>? id,
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(number);

        UserProfileId = userProfileId;
        Number = number;
        IsMain = isMain;
    }

    public static Phone Create(
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        Id<Phone>? id = null)
    {
        return new Phone(id, userProfileId, number, isMain);
    }

    public static Phone Rehydrate(
        Id<UserProfile> userProfileId,
        PhoneNumber number,
        bool isMain = false,
        Id<Phone>? id = null)
    {
        return new Phone(id, userProfileId, number, isMain);
    }

    internal void SetMain(bool isMain)
    {
        IsMain = isMain;
    }
}

