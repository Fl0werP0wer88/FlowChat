using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Common.Contracts;
using FlowChat.SocialGraphService.Domain.Events;

namespace FlowChat.SocialGraphService.Domain.Entities
{
    public class UserSocialGraph : AggregateRootBase<UserSocialGraph>
    {
        private readonly List<Contact> _contacts = [];
        private readonly List<Invitation> _invitations = [];

        public IReadOnlyList<Contact> Contacts => _contacts.AsReadOnly();
        public IReadOnlyList<Invitation> Invitations => _invitations.AsReadOnly();
        public Id<UserSocialGraph> UserId { get; }
        public string? FirstName { get; }
        public string? LastName { get; }
        public string Login { get; }
        public string? PhoneNumber { get; }
        public string? Email { get; }
        public bool IsPhoneVisible { get; }
        public bool IsEmailVisible { get; }

        public UserSocialGraph(
            Id<UserSocialGraph> id,
            string login,
            Id<UserSocialGraph>? userId = null,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null,
            bool isPhoneVisible = false,
            bool isEmailVisible = false) : base(id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(login);

            UserId = userId ?? id;
            FirstName = firstName;
            LastName = lastName;
            Login = login;
            PhoneNumber = phoneNumber;
            Email = email;
            IsPhoneVisible = isPhoneVisible;
            IsEmailVisible = isEmailVisible;
        }

        public UserSocialGraph(
            Guid id,
            string login,
            Guid? userId = null,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null,
            bool isPhoneVisible = false,
            bool isEmailVisible = false)
            : this(
                Id<UserSocialGraph>.FromGuid(id),
                login,
                userId.HasValue ? Id<UserSocialGraph>.FromGuid(userId.Value) : null,
                firstName,
                lastName,
                phoneNumber,
                email,
                isPhoneVisible,
                isEmailVisible)
        {

        }

        public Invitation SendInvitation(Invitation invitation)
        {
            ArgumentNullException.ThrowIfNull(invitation);

            if (_invitations.Any(x => x.Id == invitation.Id))
            {
                throw new InvalidOperationException($"Invitation '{invitation.Id.Value}' already exists.");
            }

            _invitations.Add(invitation);
            AddDomainEvent(new InvitationSentDomainEvent(
                Id,
                invitation.Id,
                invitation.RequesterId,
                invitation.AddresseeId));

            return invitation;
        }

        public Contact AcceptInvitation(
            Id<Invitation> invitationId,
            string login,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null)
        {
            var invitation = _invitations.FirstOrDefault(x => x.Id == invitationId)
                ?? throw new InvalidOperationException($"Invitation '{invitationId.Value}' was not found.");

            invitation.Accept();

            var ownerUserId = UserId.Value;
            var contactUserId = invitation.RequesterId == ownerUserId
                ? invitation.AddresseeId
                : invitation.RequesterId;

            if (_contacts.Any(x => x.OwnerUserId == ownerUserId && x.ContactUserId == contactUserId))
            {
                throw new InvalidOperationException("Contact relationship already exists.");
            }

            var contact = Contact.Create(
                Id<Contact>.New(),
                ownerUserId,
                contactUserId,
                login,
                firstName,
                lastName,
                phoneNumber,
                email);
            _contacts.Add(contact);

            AddDomainEvent(new InvitationAcceptedDomainEvent(
                Id,
                invitation.Id,
                contact.Id,
                invitation.RequesterId,
                invitation.AddresseeId));

            return contact;
        }
    }
}
