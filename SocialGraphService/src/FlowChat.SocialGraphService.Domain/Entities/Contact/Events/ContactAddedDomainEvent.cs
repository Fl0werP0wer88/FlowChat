using FlowChat.Shared.Domain;

namespace FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

public sealed class ContactAddedDomainEvent(
    Id<Contact> aggregateId,
    Guid ownerUserId,
    Guid contactUserId) : BaseContactDomainEvent(aggregateId)
{
    public Guid OwnerUserId { get; } = ownerUserId;
    public Guid ContactUserId { get; } = contactUserId;
}
