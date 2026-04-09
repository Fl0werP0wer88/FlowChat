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
    public string DisplayName { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Organization { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public bool IsActive { get; private set; }
    public UtcDateTimeOffset? LastSeenAtUtc { get; private set; }
    public bool IsEmailVisible { get; private set; }
    public bool IsPhoneVisible { get; private set; }
    public IReadOnlyList<Email> Emails => _emails.AsReadOnly();
    public IReadOnlyList<Phone> Phones => _phones.AsReadOnly();


    private UserProfile(
        Id<UserProfile>? id,
        string friendlyUserId,
        string normalizedFriendlyUserId,
        string displayName,
        string? firstName = null,
        string? lastName = null,
        string? organization = null,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        UtcDateTimeOffset? lastSeenAtUtc = null,
        bool isEmailVisible = true,
        bool isPhoneVisible = true) : base(id)
    {
        FriendlyUserId = friendlyUserId;
        NormalizedFriendlyUserId = normalizedFriendlyUserId;
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        Organization = organization;
        AvatarUrl = avatarUrl;
        Bio = bio;
        IsActive = isActive;
        LastSeenAtUtc = lastSeenAtUtc;
        IsEmailVisible = isEmailVisible;
        IsPhoneVisible = isPhoneVisible;
    }

    public static UserProfile Create(
        string friendlyUserId,
        string displayName,
        EmailAddress emailAddress,
        PhoneNumber? phoneNumber = null,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        UtcDateTimeOffset? lastSeenAtUtc = null,
        bool isEmailVisible = true,
        bool isPhoneVisible = true,
        Id<UserProfile>? id = null,
        string? firstName = null,
        string? lastName = null,
        string? organization = null)
    {
        var typedId = id ?? Id<UserProfile>.New();
        var normalizedFriendlyUserId = NormalizeRequired(friendlyUserId, nameof(friendlyUserId));
        var normalizedDisplayName = NormalizeRequired(displayName, nameof(displayName));
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
            normalizedDisplayName,
            normalizedFirstName,
            normalizedLastName,
            normalizedOrganization,
            normalizedAvatarUrl,
            normalizedBio,
            isActive,
            lastSeenAtUtc,
            isEmailVisible,
            isPhoneVisible);

        // Suppress intermediate snapshots while building initial state — a single snapshot
        // is emitted below after the invariant check confirms the aggregate is fully valid.
        var initialEmail = userProfile.AddEmailInternal(emailAddress, shouldMarkAggregateStateChanged: false);

        if (phoneNumber is not null)
        {
            userProfile.AddPhoneInternal(phoneNumber, shouldMarkAggregateStateChanged: false);
        }

        // Validate after all contacts are added, not per-add, because AddEmailInternal auto-assigns
        // IsMain/IsAuth to the first email and these flags must be consistent as a group.
        EnsureInitialContactInvariant(userProfile._emails, userProfile._phones);

        var currentMainPhone = userProfile.Phones.FirstOrDefault(x => x.IsMain)?.Number;

        userProfile.AddDomainEvent(new UserProfileCreatedDomainEvent(
            userProfile.Id,
            initialEmail.Id,
            userProfile.FriendlyUserId,
            userProfile.DisplayName,
            initialEmail.Address,
            currentMainPhone,
            userProfile.AvatarUrl,
            userProfile.Bio,
            userProfile.IsActive,
            userProfile.LastSeenAtUtc,
            userProfile.IsEmailVisible,
            userProfile.IsPhoneVisible,
            userProfile.FirstName,
            userProfile.LastName,
            userProfile.Organization));

        return userProfile;
    }

    public Email AddEmail(EmailAddress address, Id<Email>? id = null)
    {
        ArgumentNullException.ThrowIfNull(address);

        var email = AddEmailInternal(address, id, shouldMarkAggregateStateChanged: false);

        AddDomainEvent(new EmailAddedDomainEvent(Id, email.Id, email.Address));
        MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);

        return email;
    }

    public void SetMainEmail(Id<Email> emailId)
    {
        ArgumentNullException.ThrowIfNull(emailId);

        SetMainEmailInternal(emailId, shouldAddDomainEvent: true, shouldMarkAggregateStateChanged: true);
    }

    public void SetAuthEmail(Id<Email> emailId)
    {
        ArgumentNullException.ThrowIfNull(emailId);

        SetAuthEmailInternal(emailId, shouldMarkAggregateStateChanged: true);
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
        return AddPhoneInternal(number, id);
    }

    public void SetMainPhone(Id<Phone> phoneId)
    {
        ArgumentNullException.ThrowIfNull(phoneId);

        SetMainPhoneInternal(phoneId);
    }

    private Email AddEmailInternal(
        EmailAddress normalizedAddress,
        Id<Email>? id = null,
        bool shouldMarkAggregateStateChanged = true)
    {
        ArgumentNullException.ThrowIfNull(normalizedAddress);

        if (_emails.Any(x => x.Address == normalizedAddress))
        {
            throw new InvalidOperationException($"Email '{normalizedAddress.Value}' already exists.");
        }

        // First email added becomes both the main (display) and auth (login) email automatically.
        var shouldBeMainEmail = !_emails.Any(x => x.IsMain);
        var shouldBeAuthEmail = !_emails.Any(x => x.IsAuth);
        var email = Email.Create(Id, normalizedAddress, isMain: shouldBeMainEmail, isAuth: shouldBeAuthEmail, id: id);
        _emails.Add(email);

        if (shouldMarkAggregateStateChanged)
        {
            MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
        }

        return email;
    }

    private void SetMainEmailInternal(
        Id<Email> emailId,
        bool shouldAddDomainEvent = true,
        bool shouldMarkAggregateStateChanged = true)
    {
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

        if (shouldAddDomainEvent)
        {
            AddDomainEvent(new MainEmailChangedDomainEvent(Id, targetEmail.Id, targetEmail.Address));
        }

        if (shouldMarkAggregateStateChanged)
        {
            MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
        }
    }

    private void SetAuthEmailInternal(
        Id<Email> emailId,
        bool shouldMarkAggregateStateChanged = true)
    {
        var targetEmail = _emails.FirstOrDefault(x => x.Id == emailId);
        if (targetEmail is null)
        {
            throw new InvalidOperationException($"Email '{emailId}' was not found.");
        }

        if (targetEmail.IsAuth)
        {
            return;
        }

        foreach (var email in _emails)
        {
            email.SetAuth(email == targetEmail);
        }

        if (shouldMarkAggregateStateChanged)
        {
            MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
        }
    }

    private Phone AddPhoneInternal(
        PhoneNumber normalizedNumber,
        Id<Phone>? id = null,
        bool shouldMarkAggregateStateChanged = true)
    {
        ArgumentNullException.ThrowIfNull(normalizedNumber);

        if (_phones.Any(x => x.Number == normalizedNumber))
        {
            throw new InvalidOperationException($"Phone '{normalizedNumber.Value}' already exists.");
        }

        var phone = Phone.Create(Id, normalizedNumber, !_phones.Any(), id);
        _phones.Add(phone);

        if (shouldMarkAggregateStateChanged)
        {
            MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
        }

        return phone;
    }

    private void SetMainPhoneInternal(
        Id<Phone> phoneId,
        bool shouldAddDomainEvent = true,
        bool shouldMarkAggregateStateChanged = true)
    {
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

        if (shouldAddDomainEvent)
        {
            AddDomainEvent(new MainPhoneChangedDomainEvent(Id, targetPhone.Id, targetPhone.Number));
        }

        if (shouldMarkAggregateStateChanged)
        {
            MarkAggregateStateChanged(UserProfileConstants.UserProfileAggregateTypeName, CreateSnapshot);
        }
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
            DisplayName,
            mainEmailAddress,
            isMainEmailConfirmed,
            mainPhone,
            AvatarUrl,
            Bio,
            IsActive,
            LastSeenAtUtc,
            IsEmailVisible,
            IsPhoneVisible,
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
