using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.SocialGraphService.Domain.Entities.Contact;

public class Contact : AggregateRootBase<Contact>
{
    public Guid OwnerUserId { get; }
    public Guid ContactUserId { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string DisplayName { get; }
    public PhoneNumber? PhoneNumber { get; }
    public EmailAddress? EmailAddress { get; }
    public bool IsBlocked { get; }

    private Contact(
        Id<Contact>? id,
        Guid ownerUserId,
        Guid contactUserId,
        string displayName,
        string? firstName = null,
        string? lastName = null,
        PhoneNumber? phoneNumber = null,
        EmailAddress? emailAddress = null,
        bool isBlocked = false) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(ownerUserId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(contactUserId, Guid.Empty);

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
        Guid ownerUserId,
        Guid contactUserId,
        string displayName,
        string? firstName = null,
        string? lastName = null,
        PhoneNumber? phoneNumber = null,
        EmailAddress? emailAddress = null,
        bool isBlocked = false,
        Id<Contact>? id = null)
    {
        return new Contact(id, ownerUserId, contactUserId, displayName, firstName, lastName, phoneNumber, emailAddress, isBlocked);
    }

    public static Contact Rehydrate(
        Guid ownerUserId,
        Guid contactUserId,
        string displayName,
        string? firstName = null,
        string? lastName = null,
        PhoneNumber? phoneNumber = null,
        EmailAddress? emailAddress = null,
        bool isBlocked = false,
        Id<Contact>? id = null)
    {
        return new Contact(id, ownerUserId, contactUserId, displayName, firstName, lastName, phoneNumber, emailAddress, isBlocked);
    }
}

