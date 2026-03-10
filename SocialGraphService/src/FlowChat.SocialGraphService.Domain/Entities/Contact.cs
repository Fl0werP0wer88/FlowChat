using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Common.Constants;

namespace FlowChat.SocialGraphService.Domain.Entities;

public class Contact : AggregateRootBase<Contact>
{
    public Guid OwnerUserId { get; }
    public Guid ContactUserId { get; }
    public string? FirstName { get; }
    public string? LastName { get; }
    public string Login { get; }
    public string? PhoneNumber { get; }
    public string? Email { get; }
    public bool IsBlocked { get; }

    private Contact(
        Id<Contact>? id,
        Guid ownerUserId,
        Guid contactUserId,
        string login,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        string? email = null,
        bool isBlocked = false) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(ownerUserId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(contactUserId, Guid.Empty);

        if (ownerUserId == contactUserId)
        {
            throw new ArgumentException("OwnerUserId and ContactUserId must be different.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(login);

        OwnerUserId = ownerUserId;
        ContactUserId = contactUserId;
        FirstName = firstName;
        LastName = lastName;
        Login = login;
        PhoneNumber = phoneNumber;
        Email = email;
        IsBlocked = isBlocked;
    }


    public static Contact Create(
        Guid ownerUserId,
        Guid contactUserId,
        string login,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        string? email = null,
        bool isBlocked = false,
        Id<Contact>? id = null)
    {
        return new Contact(id, ownerUserId, contactUserId, login, firstName, lastName, phoneNumber, email, isBlocked);
    }

    public static Contact Rehydrate(
        Guid ownerUserId,
        Guid contactUserId,
        string login,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null,
        string? email = null,
        bool isBlocked = false,
        Id<Contact>? id = null)
    {
        return new Contact(id, ownerUserId, contactUserId, login, firstName, lastName, phoneNumber, email, isBlocked);
    }
}
