using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Domain.Common.Constants;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Domain.Entities;

public class UserProfile : AggregateRootBase<UserProfile>
{
    private readonly List<Email> _emails = [];
    private readonly List<Phone> _phones = [];

    public string UserName { get; private set; }
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }
    public bool IsEmailVisible { get; private set; }
    public bool IsPhoneVisible { get; private set; }
    public IReadOnlyList<Email> Emails => _emails.AsReadOnly();
    public IReadOnlyList<Phone> Phones => _phones.AsReadOnly();

    private UserProfile(
        Id<UserProfile>? id,
        string userName,
        string displayName,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        DateTime? lastSeenAtUtc = null,
        bool isEmailVisible = true,
        bool isPhoneVisible = true,
        IEnumerable<Email>? emails = null,
        IEnumerable<Phone>? phones = null) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        UserName = userName.Trim();
        DisplayName = displayName.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        IsActive = isActive;
        LastSeenAtUtc = lastSeenAtUtc;
        IsEmailVisible = isEmailVisible;
        IsPhoneVisible = isPhoneVisible;
        _emails.AddRange(emails ?? []);
        _phones.AddRange(phones ?? []);
    }

    public static UserProfile Create(
        string userName,
        string displayName,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        DateTime? lastSeenAtUtc = null,
        bool isEmailVisible = true,
        bool isPhoneVisible = true,
        IEnumerable<Email>? emails = null,
        IEnumerable<Phone>? phones = null,
        Id<UserProfile>? id = null)
    {
        var typedId = id ?? Id<UserProfile>.New();
        var emailList = (emails ?? []).ToList();
        var phoneList = (phones ?? []).ToList();

        EnsureInitialContactInvariant(emailList, phoneList);

        var userProfile = new UserProfile(
            typedId,
            userName,
            displayName,
            avatarUrl,
            bio,
            isActive,
            lastSeenAtUtc,
            isEmailVisible,
            isPhoneVisible,
            emailList,
            phoneList);

        var mainEmail = userProfile.Emails.FirstOrDefault(x => x.IsMain)?.Address;
        var mainPhone = userProfile.Phones.FirstOrDefault(x => x.IsMain)?.Number;

        userProfile.AddDomainEvent(new UserProfileCreatedDomainEvent(
            userProfile.Id,
            userProfile.UserName,
            userProfile.DisplayName,
            mainEmail,
            mainPhone,
            userProfile.AvatarUrl,
            userProfile.Bio,
            userProfile.IsActive,
            userProfile.LastSeenAtUtc,
            userProfile.IsEmailVisible,
            userProfile.IsPhoneVisible));

        return userProfile;
    }

    public static UserProfile Rehydrate(
        string userName,
        string displayName,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        DateTime? lastSeenAtUtc = null,
        bool isEmailVisible = true,
        bool isPhoneVisible = true,
        IEnumerable<Email>? emails = null,
        IEnumerable<Phone>? phones = null,
        Id<UserProfile>? id = null)
    {
        return new UserProfile(
            id,
            userName,
            displayName,
            avatarUrl,
            bio,
            isActive,
            lastSeenAtUtc,
            isEmailVisible,
            isPhoneVisible,
            emails,
            phones);
    }

    public Email AddEmail(string address, Id<Email>? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var normalizedAddress = address.Trim();
        if (_emails.Any(x => string.Equals(x.Address, normalizedAddress, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Email '{normalizedAddress}' already exists.");
        }

        var email = Email.Create(Id.Value, normalizedAddress, !_emails.Any(), id);
        _emails.Add(email);
        return email;
    }

    public void SetMainEmail(Guid emailId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(emailId, Guid.Empty);

        var targetEmail = _emails.FirstOrDefault(x => x.Id == Id<Email>.FromGuid(emailId));
        if (targetEmail is null)
        {
            throw new InvalidOperationException($"Email '{emailId}' was not found.");
        }

        if (targetEmail.IsMain)
        {
            return;
        }

        foreach (var email in _emails)
        {
            email.SetMain(email == targetEmail);
        }

        AddDomainEvent(new MainEmailChangedDomainEvent(Id, targetEmail.Id, targetEmail.Address));
    }

    public Phone AddPhone(string number, Id<Phone>? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        var normalizedNumber = number.Trim();
        if (_phones.Any(x => string.Equals(x.Number, normalizedNumber, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Phone '{normalizedNumber}' already exists.");
        }

        var phone = Phone.Create(Id.Value, normalizedNumber, !_phones.Any(), id);
        _phones.Add(phone);
        return phone;
    }

    public void SetMainPhone(Guid phoneId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(phoneId, Guid.Empty);

        var targetPhone = _phones.FirstOrDefault(x => x.Id == Id<Phone>.FromGuid(phoneId));
        if (targetPhone is null)
        {
            throw new InvalidOperationException($"Phone '{phoneId}' was not found.");
        }

        if (targetPhone.IsMain)
        {
            return;
        }

        foreach (var phone in _phones)
        {
            phone.SetMain(phone == targetPhone);
        }

        AddDomainEvent(new MainPhoneChangedDomainEvent(Id, targetPhone.Id, targetPhone.Number));
    }

    private static void EnsureInitialContactInvariant(
        IReadOnlyCollection<Email> emails,
        IReadOnlyCollection<Phone> phones)
    {
        var mainEmailCount = emails.Count(x => x.IsMain);
        var mainPhoneCount = phones.Count(x => x.IsMain);

        if (mainEmailCount > 1)
        {
            throw new InvalidOperationException("User profile cannot have more than one main email.");
        }

        if (mainPhoneCount > 1)
        {
            throw new InvalidOperationException("User profile cannot have more than one main phone.");
        }

        if (mainEmailCount == 0 && mainPhoneCount == 0)
        {
            throw new InvalidOperationException("User profile must have at least one main email or main phone.");
        }
    }
}
