using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Events.Contracts;

namespace FlowChat.SocialGraphService.Domain.Events;

public sealed class InvitationSentDomainEvent : BaseUserSocialGraphDomainEvent
{
    public Id<Invitation> InvitationId { get; }
    public Guid RequesterId { get; }
    public Guid AddresseeId { get; }

    public InvitationSentDomainEvent(
        Id<UserSocialGraph> aggregateId,
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
