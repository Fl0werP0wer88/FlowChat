using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.Domain.Entities.Contact;

public class Contact : AggregateRootBase<Contact>
{
    public Id<UserProfileMarker> OwnerUserId { get; }
    public Id<UserProfileMarker> ContactUserId { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string DisplayName { get; }
    public PhoneNumber? PhoneNumber { get; }
    public EmailAddress? EmailAddress { get; }
    public bool IsBlocked { get; }

    private Contact(
        Id<Contact> id,
        Id<UserProfileMarker> ownerUserId,
        Id<UserProfileMarker> contactUserId,
        string displayName,
        string? firstName = null,
        string? lastName = null,
        PhoneNumber? phoneNumber = null,
        EmailAddress? emailAddress = null,
        bool isBlocked = false) : base(id)
    {
        ArgumentNullException.ThrowIfNull(ownerUserId);
        ArgumentNullException.ThrowIfNull(contactUserId);

        if (ownerUserId == contactUserId)
        {
            throw new ArgumentException("OwnerUserId and ContactUserId must be different.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        OwnerUserId = ownerUserId;
        ContactUserId = contactUserId;
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        PhoneNumber = phoneNumber;
        EmailAddress = emailAddress;
        IsBlocked = isBlocked;
    }


    public static Contact Create(
        Id<Contact> id,
        Id<UserProfileMarker> ownerUserId,
        Id<UserProfileMarker> contactUserId,
        string displayName,
        string? firstName = null,
        string? lastName = null,
        PhoneNumber? phoneNumber = null,
        EmailAddress? emailAddress = null,
        bool isBlocked = false)
    {
        ArgumentNullException.ThrowIfNull(id);
        var contact = new Contact(
            id,
            ownerUserId,
            contactUserId,
            displayName,
            firstName,
            lastName,
            phoneNumber,
            emailAddress,
            isBlocked);

        return contact;
    }

    public static Contact Rehydrate(
        Id<UserProfileMarker> ownerUserId,
        Id<UserProfileMarker> contactUserId,
        string displayName,
        string? firstName = null,
        string? lastName = null,
        PhoneNumber? phoneNumber = null,
        EmailAddress? emailAddress = null,
        bool isBlocked = false,
        Id<Contact>? id = null)
    {
        return new Contact(id ?? Id<Contact>.New(), ownerUserId, contactUserId, displayName, firstName, lastName, phoneNumber, emailAddress, isBlocked);
    }
}

