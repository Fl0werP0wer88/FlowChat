using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile;

public class UserProfile : AggregateRootBase<UserProfile>
{
    private readonly List<Email> _emails = [];
    private readonly List<Phone> _phones = [];

    public FriendlyUserId FriendlyUserId { get; private set; }
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
        Id<UserProfile> id,
        FriendlyUserId friendlyUserId,
        string? firstName = null,
        string? lastName = null,
        string? organization = null,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        UtcDateTimeOffset? lastSeenAtUtc = null) : base(id)
    {
        FriendlyUserId = friendlyUserId;
        FirstName = firstName;
        LastName = lastName;
        Organization = organization;
        AvatarUrl = avatarUrl;
        Bio = bio;
        IsActive = isActive;
        LastSeenAtUtc = lastSeenAtUtc;
    }

    public static UserProfile Create(
        Id<UserProfile> id,
        string friendlyUserId,
        EmailAddress emailAddress,
        PhoneNumber? phoneNumber = null,
        string? avatarUrl = null,
        string? bio = null,
        bool isActive = true,
        UtcDateTimeOffset? lastSeenAtUtc = null,
        string? firstName = null,
        string? lastName = null,
        string? organization = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        var typedId = id;
        var normalizedFriendlyUserId = FriendlyUserId.Create(friendlyUserId);
        var normalizedFirstName = NormalizeOptional(firstName);
        var normalizedLastName = NormalizeOptional(lastName);
        var normalizedOrganization = NormalizeOptional(organization);
        var normalizedAvatarUrl = NormalizeOptional(avatarUrl);
        var normalizedBio = NormalizeOptional(bio);

        var userProfile = new UserProfile(
            typedId,
            normalizedFriendlyUserId,
            normalizedFirstName,
            normalizedLastName,
            normalizedOrganization,
            normalizedAvatarUrl,
            normalizedBio,
            isActive,
            lastSeenAtUtc);

        var initialEmail = Email.Create(Id<Email>.New(), userProfile.Id, emailAddress, isMain: true, isAuth: true);
        userProfile._emails.Add(initialEmail);

        if (phoneNumber is not null)
        {
            var initialPhone = Phone.Create(Id<Phone>.New(), userProfile.Id, phoneNumber, isMain: true);
            userProfile._phones.Add(initialPhone);
        }

        EnsureInitialContactInvariant(userProfile._emails, userProfile._phones);

        var currentMainPhone = userProfile.Phones.FirstOrDefault(x => x.IsMain);

        userProfile.AddDomainEvent(new UserProfileCreatedDomainEvent(
            userProfile.Id,
            initialEmail.Id,
            currentMainPhone?.Id,
            userProfile.FriendlyUserId.Value,
            initialEmail.Address,
            currentMainPhone?.Number,
            userProfile.AvatarUrl,
            userProfile.Bio,
            userProfile.IsActive,
            userProfile.LastSeenAtUtc,
            userProfile.FirstName,
            userProfile.LastName,
            userProfile.Organization));

        return userProfile;
    }

    public Email AddEmail(Id<Email> id, EmailAddress address)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(address);

        if (_emails.Any(x => x.Address == address))
        {
            throw new InvalidOperationException($"Email '{address.Value}' already exists.");
        }

        var shouldBeMainEmail = !_emails.Any(x => x.IsMain);
        var shouldBeAuthEmail = !_emails.Any(x => x.IsAuth);
        var email = Email.Create(id, Id, address, isMain: shouldBeMainEmail, isAuth: shouldBeAuthEmail);
        _emails.Add(email);

        AddDomainEvent(new EmailAddedDomainEvent(Id, email.Id, email.Address));

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

        AddDomainEvent(new AuthEmailChangedDomainEvent(Id, targetEmail.Id, targetEmail.Address));
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
    }

    public Phone AddPhone(Id<Phone> id, PhoneNumber number)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(number);

        if (_phones.Any(x => x.Number == number))
        {
            throw new InvalidOperationException($"Phone '{number.Value}' already exists.");
        }

        var phone = Phone.Create(id, Id, number, isMain: !_phones.Any());
        _phones.Add(phone);

        return phone;
    }

    public void UpdateProfile(
        string? firstName,
        string? lastName,
        string? organization,
        string? avatarUrl,
        string? bio,
        bool isActive)
    {
        var normalizedFirstName = NormalizeOptional(firstName);
        var normalizedLastName = NormalizeOptional(lastName);
        var normalizedOrganization = NormalizeOptional(organization);
        var normalizedAvatarUrl = NormalizeOptional(avatarUrl);
        var normalizedBio = NormalizeOptional(bio);

        FirstName = normalizedFirstName;
        LastName = normalizedLastName;
        Organization = normalizedOrganization;
        AvatarUrl = normalizedAvatarUrl;
        Bio = normalizedBio;
        IsActive = isActive;
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

    }

    private static string NormalizeRequired(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value.Trim();
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
