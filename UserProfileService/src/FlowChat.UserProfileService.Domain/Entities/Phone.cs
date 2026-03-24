using FlowChat.Domain.Abstractions;
using FlowChat.Domain.Abstractions.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities;

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
        string number,
        bool isMain = false,
        Id<Phone>? id = null)
    {
        return new Phone(id, userProfileId, PhoneNumber.Create(number), isMain);
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
        string number,
        bool isMain = false,
        Id<Phone>? id = null)
    {
        return new Phone(id, userProfileId, PhoneNumber.Create(number), isMain);
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
