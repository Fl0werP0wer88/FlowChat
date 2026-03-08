using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Enums;

namespace FlowChat.SocialGraphService.Domain.Entities
{
    public class UserSocialGraph : AggregateRootBase<UserSocialGraph>
    {
        private readonly List<Contact> _contacts = [];

        public IReadOnlyList<Contact> Contacts => _contacts.AsReadOnly();
        public Guid UserId { get; }
        public string? FirstName { get; }
        public string? LastName { get; }
        public string Login { get; }
        public string? PhoneNumber { get; }
        public string? Email { get; }
        public bool IsPhoneVisible { get; }
        public bool IsEmailVisible { get; }

        private UserSocialGraph(
            Id<UserSocialGraph>? id,
            string login,
            Guid? userId = null,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null,
            bool isPhoneVisible = false,
            bool isEmailVisible = false,
            IEnumerable<Contact>? contacts = null) : base(id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(login);

            UserId = userId ?? Id.Value;
            FirstName = firstName;
            LastName = lastName;
            Login = login;
            PhoneNumber = phoneNumber;
            Email = email;
            IsPhoneVisible = isPhoneVisible;
            IsEmailVisible = isEmailVisible;
            _contacts.AddRange(contacts ?? []);
        }

        public static UserSocialGraph Create(
            string login,
            Guid? userId = null,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null,
            bool isPhoneVisible = false,
            bool isEmailVisible = false,
            IEnumerable<Contact>? contacts = null,
            Id<UserSocialGraph>? id = null)
        {
            return new UserSocialGraph(
                id,
                login,
                userId,
                firstName,
                lastName,
                phoneNumber,
                email,
                isPhoneVisible,
                isEmailVisible,
                contacts);
        }

        public Contact CreateContactFromInvitation(
            Invitation invitation,
            string login,
            string? firstName = null,
            string? lastName = null,
            string? phoneNumber = null,
            string? email = null)
        {
            ArgumentNullException.ThrowIfNull(invitation);

            if (invitation.Status != InvitationStatus.Accepted)
            {
                throw new InvalidOperationException("Only accepted invitations can create contacts.");
            }

            var ownerUserId = UserId;

            if (invitation.RequesterId != ownerUserId && invitation.AddresseeId != ownerUserId)
            {
                throw new InvalidOperationException("Invitation does not belong to this social graph.");
            }

            var contactUserId = invitation.RequesterId == ownerUserId
                ? invitation.AddresseeId
                : invitation.RequesterId;

            if (_contacts.Any(x => x.OwnerUserId == ownerUserId && x.ContactUserId == contactUserId))
            {
                throw new InvalidOperationException("Contact relationship already exists.");
            }

            var contact = Contact.Create(
                ownerUserId,
                contactUserId,
                login,
                firstName,
                lastName,
                phoneNumber,
                email,
                id: Id<Contact>.New());
            _contacts.Add(contact);

            return contact;
        }
    }
}
