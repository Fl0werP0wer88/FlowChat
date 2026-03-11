using FlowChat.Domain.Abstractions;

namespace FlowChat.UserProfileService.Domain.Entities;

public class Phone : EntityBase<Phone>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public string Number { get; private set; }
    public bool IsMain { get; private set; }

    private Phone(
        Id<Phone>? id,
        Id<UserProfile> userProfileId,
        string number,
        bool isMain = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        UserProfileId = userProfileId;
        Number = number.Trim();
        IsMain = isMain;
    }

    public static Phone Create(
        Id<UserProfile> userProfileId,
        string number,
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
        return new Phone(id, userProfileId, number, isMain);
    }

    internal void SetMain(bool isMain)
    {
        IsMain = isMain;
    }
}
