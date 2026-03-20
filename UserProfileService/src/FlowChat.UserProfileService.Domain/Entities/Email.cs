using System.Net.Mail;
using FlowChat.Domain.Abstractions;

namespace FlowChat.UserProfileService.Domain.Entities;

public class Email : EntityBase<Email>
{
    public Id<UserProfile> UserProfileId { get; private set; }
    public string Address { get; private set; }
    public string NormalizedAddress { get; private set; }
    public bool IsMain { get; private set; }

    private Email(
        Id<Email>? id,
        Id<UserProfile> userProfileId,
        string address,
        bool isMain = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var trimmedAddress = address.Trim();
        ValidateEmail(trimmedAddress);

        UserProfileId = userProfileId;
        Address = trimmedAddress;
        NormalizedAddress = trimmedAddress.ToUpper();
        IsMain = isMain;
    }

    public static Email Create(
        Id<UserProfile> userProfileId,
        string address,
        bool isMain = false,
        Id<Email>? id = null)
    {
        return new Email(id, userProfileId, address, isMain);
    }

    public static Email Rehydrate(
        Id<UserProfile> userProfileId,
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

    private static void ValidateEmail(string address)
    {
        if (!MailAddress.TryCreate(address, out var parsedAddress) ||
            !string.Equals(parsedAddress.Address, address, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Address must be a valid email address.", nameof(address));
        }
    }
}
