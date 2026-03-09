using FlowChat.Domain.Abstractions;

namespace FlowChat.UserProfileService.Domain.Entities;

public class Email : EntityBase<Email>
{
    public Guid UserProfileId { get; private set; }
    public string Address { get; private set; }
    public bool IsMain { get; private set; }

    private Email(
        Id<Email>? id,
        Guid userProfileId,
        string address,
        bool isMain = false) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userProfileId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        UserProfileId = userProfileId;
        Address = address.Trim();
        IsMain = isMain;
    }

    public static Email Create(
        Guid userProfileId,
        string address,
        bool isMain = false,
        Id<Email>? id = null)
    {
        return new Email(id, userProfileId, address, isMain);
    }

    public static Email Rehydrate(
        Guid userProfileId,
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
