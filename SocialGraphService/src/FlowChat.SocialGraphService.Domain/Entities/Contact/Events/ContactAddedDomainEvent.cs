using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.Domain.Entities.Contact.Events;

public sealed class ContactAddedDomainEvent(
    Id<Contact> aggregateId,
    Id<UserProfileMarker> ownerUserId,
    Id<UserProfileMarker> contactUserId) : BaseContactDomainEvent(aggregateId)
{
    public Id<UserProfileMarker> OwnerUserId { get; } = ownerUserId;
    public Id<UserProfileMarker> ContactUserId { get; } = contactUserId;
}
