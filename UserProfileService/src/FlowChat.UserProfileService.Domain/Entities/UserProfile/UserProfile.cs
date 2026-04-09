using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Common.Constants;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public class UserProfile : AggregateRootBase<UserProfile>
{
    private readonly List<Email> _emails = [];
    private readonly List<Phone> _phones = [];

    public string FriendlyUserId { get; private set; }
    public string NormalizedFriendlyUserId { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Organization { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public bool IsActive { get; private set; }
    public UtcDateTimeOffset? LastSeenAtUtc { get; private set; }
    public IReadOnlyList<Email> Emails => _emails.AsReadOnly();
    public IReadOnlyList<Phone> Phones => _phones.AsReadOnly();


    private UserProfile(
        Id<UserProfile>? id,
        string friendlyUserId,
        string normalizedFriendlyUserId,
        string? firstName = null,
        string? lastName = null,
        string? organization = null,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        UtcDateTimeOffset? lastSeenAtUtc = null) : base(id)
    {
        FriendlyUserId = friendlyUserId;
        NormalizedFriendlyUserId = normalizedFriendlyUserId;
        FirstName = firstName;
        LastName = lastName;
        Organization = organization;
        AvatarUrl = avatarUrl;
        Bio = bio;
        IsActive = isActive;
        LastSeenAtUtc = lastSeenAtUtc;
    }

    public static UserProfile Create(
        string friendlyUserId,
        EmailAddress emailAddress,
        PhoneNumber? phoneNumber = null,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        UtcDateTimeOffset? lastSeenAtUtc = null,
        Id<UserProfile>? id = null,
        string? firstName = null,
        string? lastName = null,
        string? organization = null)
    {
        var typedId = id ?? Id<UserProfile>.New();
        var normalizedFriendlyUserId = NormalizeRequired(friendlyUserId, nameof(friendlyUserId));
        var normalizedFirstName = NormalizeOptional(firstName);
        var normalizedLastName = NormalizeOptional(lastName);
        var normalizedOrganization = NormalizeOptional(organization);
        var canonicalFriendlyUserId = NormalizeFriendlyUserId(normalizedFriendlyUserId);
        var normalizedAvatarUrl = NormalizeOptional(avatarUrl);
        var normalizedBio = NormalizeOptional(bio);

        var userProfile = new UserProfile(
            typedId,
            normalizedFriendlyUserId,
            canonicalFriendlyUserId,
            normalizedFirstName,
            normalizedLastName,
            normalizedOrganization,
            normalizedAvatarUrl,
            normalizedBio,
            isActive,
            lastSeenAtUtc);

        var initialEmail = Email.Create(userProfile.Id, emailAddress, isMain: true, isAuth: true);
        userProfile._emails.Add(initialEmail);

        if (phoneNumber is not null)
        {
            var initialPhone = Phone.Create(userProfile.Id, phoneNumber, isMain: true);
            userProfile._phones.Add(initialPhone);
        }

        EnsureInitialContactInvariant(userProfile._emails, userProfile._phones);

        var currentMainPhone = userProfile.Phones.FirstOrDefault(x => x.IsMain)?.Number;

        userProfile.AddDomainEvent(new UserProfileCreatedDomainEvent(
            userProfile.Id,
            initialEmail.Id,
            userProfile.FriendlyUserId,
            initialEmail.Address,
            currentMainPhone,
            userProfile.AvatarUrl,
            userProfile.Bio,
            userProfile.IsActive,
            userProfile.LastSeenAtUtc,
            userProfile.FirstName,
            userProfile.LastName,
            userProfile.Organization));

        return userProfile;
    }

    public Email AddEmail(EmailAddress address, Id<Email>? id = null)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (_emails.Any(x => x.Address == address))
        {
            throw new InvalidOperationException($"Email '{address.Value}' already exists.");
        }

        var shouldBeMainEmail = !_emails.Any(x => x.IsMain);
        var shouldBeAuthEmail = !_emails.Any(x => x.IsAuth);
        var email = Email.Create(Id, address, isMain: shouldBeMainEmail, isAuth: shouldBeAuthEmail, id: id);
        _emails.Add(email);

        AddDomainEvent(new EmailAddedDomainEvent(Id, email.Id, email.Address));
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

        if (!targetEmail.IsConfirmed)
        {
            throw new InvalidOperationException($"Email '{targetEmail.Address.Value}' must be confirmed before it can be set as main.");
        }

        foreach (var email in _emails)
        {
            email.SetMain(email == targetEmail);
        }

        AddDomainEvent(new MainEmailChangedDomainEvent(Id, targetEmail.Id, targetEmail.Address));
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
    }

    public void SetAuthEmail(Id<Email> emailId)
    {
        ArgumentNullException.ThrowIfNull(emailId);

        var targetEmail = _emails.FirstOrDefault(x => x.Id == emailId);
        if (targetEmail is null)
        {
            throw new InvalidOperationException($"Email '{emailId}' was not found.");
        }

        if (targetEmail.IsAuth)
        {
            return;
        }

        if (!targetEmail.IsConfirmed)
        {
            throw new InvalidOperationException($"Email '{targetEmail.Address.Value}' must be confirmed before it can be set as auth.");
        }

        foreach (var email in _emails)
        {
            email.SetAuth(email == targetEmail);
        }

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
        AddDomainEvent(new EmailConfirmedDomainEvent(Id, targetEmail.Id, targetEmail.Address, targetEmail.IsAuth));
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
    }

    public Phone AddPhone(PhoneNumber number, Id<Phone>? id = null)
    {
        ArgumentNullException.ThrowIfNull(number);

        if (_phones.Any(x => x.Number == number))
        {
            throw new InvalidOperationException($"Phone '{number.Value}' already exists.");
        }

        var phone = Phone.Create(Id, number, isMain: !_phones.Any(), id: id);
        _phones.Add(phone);

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

        if (!targetPhone.IsConfirmed)
        {
            throw new InvalidOperationException($"Phone '{targetPhone.Number.Value}' must be confirmed before it can be set as main.");
        }

        foreach (var phone in _phones)
        {
            phone.SetMain(phone == targetPhone);
        }

        AddDomainEvent(new MainPhoneChangedDomainEvent(Id, targetPhone.Id, targetPhone.Number));
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
            FriendlyUserId,
            mainEmailAddress,
            isMainEmailConfirmed,
            mainPhone,
            AvatarUrl,
            Bio,
            IsActive,
            LastSeenAtUtc,
            FirstName,
            LastName,
            Organization);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value.Trim();
    }

    private static string NormalizeFriendlyUserId(string friendlyUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyUserId);
        return friendlyUserId.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void EnsureInitialContactInvariant(
        IReadOnlyCollection<Email> emails,
        IReadOnlyCollection<Phone> phones)
    {
        var mainEmailCount = emails.Count(x => x.IsMain);
        var authEmailCount = emails.Count(x => x.IsAuth);
        var mainPhoneCount = phones.Count(x => x.IsMain);

        if (mainEmailCount > 1)
        {
            throw new InvalidOperationException("User profile cannot have more than one main email.");
        }

        if (mainEmailCount == 0)
        {
            throw new InvalidOperationException("User profile must have exactly one main email.");
        }

        if (authEmailCount > 1)
        {
            throw new InvalidOperationException("User profile cannot have more than one auth email.");
        }

        if (authEmailCount == 0)
        {
            throw new InvalidOperationException("User profile must have exactly one auth email.");
        }

        if (mainPhoneCount > 1)
        {
            throw new InvalidOperationException("User profile cannot have more than one main phone.");
        }

    }
}
