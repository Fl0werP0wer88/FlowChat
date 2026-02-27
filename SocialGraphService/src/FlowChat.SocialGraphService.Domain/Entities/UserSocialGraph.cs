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
        public Id<UserSocialGraph> UserId { get; } = Id<UserSocialGraph>.New();
        public UserSocialGraph(Id<UserSocialGraph> id) : base(id)
        {

        }

        public UserSocialGraph(Guid id) : this(Id<UserSocialGraph>.FromGuid(id))
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

        public Contact AcceptInvitation(Id<Invitation> invitationId)
        {
            var invitation = _invitations.FirstOrDefault(x => x.Id == invitationId)
                ?? throw new InvalidOperationException($"Invitation '{invitationId.Value}' was not found.");

            invitation.Accept();

            var (userId1, userId2) = NormalizePair(invitation.RequesterId, invitation.AddresseeId);

            if (_contacts.Any(x => x.UserId1 == userId1 && x.UserId2 == userId2))
            {
                throw new InvalidOperationException("Contact relationship already exists.");
            }

            var contact = Contact.Create(Id<Contact>.New(), userId1, userId2);
            _contacts.Add(contact);

            AddDomainEvent(new InvitationAcceptedDomainEvent(
                Id,
                invitation.Id,
                contact.Id,
                invitation.RequesterId,
                invitation.AddresseeId));

            return contact;
        }

        private static (Guid UserId1, Guid UserId2) NormalizePair(Guid userAId, Guid userBId)
        {
            return userAId.CompareTo(userBId) <= 0
                ? (userAId, userBId)
                : (userBId, userAId);
        }
    }
}
