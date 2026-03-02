using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Events.Contracts;

namespace FlowChat.SocialGraphService.Domain.Events;

public sealed class InvitationAcceptedDomainEvent : BaseUserSocialGraphDomainEvent
{
    public Id<Invitation> InvitationId { get; }
    public Id<Contact> ContactId { get; }
    public Guid RequesterId { get; }
    public Guid AddresseeId { get; }

    public InvitationAcceptedDomainEvent(
        Id<UserSocialGraph> aggregateId,
        Id<Invitation> invitationId,
        Id<Contact> contactId,
        Guid requesterId,
        Guid addresseeId)
        : base(aggregateId)
    {
        InvitationId = invitationId;
        ContactId = contactId;
        RequesterId = requesterId;
        AddresseeId = addresseeId;
    }
}
