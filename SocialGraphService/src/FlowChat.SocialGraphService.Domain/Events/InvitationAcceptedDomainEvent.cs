using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Events.Contracts;

namespace FlowChat.SocialGraphService.Domain.Events;

public sealed class InvitationAcceptedDomainEvent : BaseInvitationDomainEvent
{
    public Id<Invitation> InvitationId { get; }
    public Guid RequesterId { get; }
    public Guid AddresseeId { get; }

    public InvitationAcceptedDomainEvent(
        Id<Invitation> aggregateId,
        Id<Invitation> invitationId,
        Guid requesterId,
        Guid addresseeId)
        : base(aggregateId)
    {
        InvitationId = invitationId;
        RequesterId = requesterId;
        AddresseeId = addresseeId;
    }
}
