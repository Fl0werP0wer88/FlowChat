using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Common.Constants;
using FlowChat.UserProfileService.Domain.Events;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities;

public class UserProfile : AggregateRootBase<UserProfile>
{
    private readonly List<Email> _emails = [];
    private readonly List<Phone> _phones = [];

    public string UserName { get; private set; }
    public string NormalizedUserName { get; private set; }
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
        bool isPhoneVisible = true) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        UserName = userName.Trim();
        NormalizedUserName = userName.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        IsActive = isActive;
        LastSeenAtUtc = lastSeenAtUtc;
        IsEmailVisible = isEmailVisible;
        IsPhoneVisible = isPhoneVisible;
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
            isPhoneVisible);

        userProfile._emails.AddRange(emailList);
        userProfile._phones.AddRange(phoneList);

        var mainEmail = userProfile.Emails.FirstOrDefault(x => x.IsMain)?.Address.Value;
        var mainPhone = userProfile.Phones.FirstOrDefault(x => x.IsMain)?.Number.Value;

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
        userProfile.MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, userProfile.CreateSnapshot);

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
        var userProfile = new UserProfile(
            id,
            userName,
            displayName,
            avatarUrl,
            bio,
            isActive,
            lastSeenAtUtc,
            isEmailVisible,
            isPhoneVisible);

        userProfile._emails.AddRange(emails ?? []);
        userProfile._phones.AddRange(phones ?? []);

        return userProfile;
    }

    public Email AddEmail(string address, Id<Email>? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var normalizedAddress = EmailAddress.Create(address);
        if (_emails.Any(x => x.Address == normalizedAddress))
        {
            throw new InvalidOperationException($"Email '{normalizedAddress.Value}' already exists.");
        }

        var email = Email.Create(Id, normalizedAddress, !_emails.Any(), id);
        _emails.Add(email);
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);

        return email;
    }

    public void SetMainEmail(Id<Email> emailId)
    {
        ArgumentNullException.ThrowIfNull(emailId);

        var targetEmail = _emails.FirstOrDefault(x => x.Id == emailId);
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

        AddDomainEvent(new MainEmailChangedDomainEvent(Id, targetEmail.Id, targetEmail.Address.Value));
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
    }

    public void ConfirmEmail(Id<Email> emailId)
    {
        ArgumentNullException.ThrowIfNull(emailId);

        var targetEmail = _emails.FirstOrDefault(x => x.Id == emailId);
        if (targetEmail is null)
        {
            throw new InvalidOperationException($"Email '{emailId}' was not found.");
        }

        if (targetEmail.IsConfirmed)
        {
            return;
        }

        targetEmail.Confirm();
        AddDomainEvent(new EmailConfirmedDomainEvent(Id, targetEmail.Id, targetEmail.Address.Value));
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
    }

    public Phone AddPhone(string number, Id<Phone>? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        var normalizedNumber = PhoneNumber.Create(number);
        if (_phones.Any(x => x.Number == normalizedNumber))
        {
            throw new InvalidOperationException($"Phone '{normalizedNumber.Value}' already exists.");
        }

        var phone = Phone.Create(Id, normalizedNumber, !_phones.Any(), id);
        _phones.Add(phone);
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);

        return phone;
    }

    public void SetMainPhone(Id<Phone> phoneId)
    {
        ArgumentNullException.ThrowIfNull(phoneId);

        var targetPhone = _phones.FirstOrDefault(x => x.Id == phoneId);
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

        AddDomainEvent(new MainPhoneChangedDomainEvent(Id, targetPhone.Id, targetPhone.Number.Value));
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
    }

    private UserProfileSnapshot CreateSnapshot()
    {
        var mainEmail = _emails.FirstOrDefault(x => x.IsMain);
        var mainEmailAddress = mainEmail?.Address.Value;
        bool? isMainEmailConfirmed = mainEmail?.IsConfirmed;
        var mainPhone = _phones.FirstOrDefault(x => x.IsMain)?.Number.Value;

        return new UserProfileSnapshot(
            Id.Value,
            UserName,
            DisplayName,
            mainEmailAddress,
            isMainEmailConfirmed,
            mainPhone,
            AvatarUrl,
            Bio,
            IsActive,
            LastSeenAtUtc,
            IsEmailVisible,
            IsPhoneVisible);
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

