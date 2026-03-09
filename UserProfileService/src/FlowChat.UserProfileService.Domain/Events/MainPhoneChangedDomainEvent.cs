using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events.Contracts;

namespace FlowChat.UserProfileService.Domain.Events;

public sealed class MainPhoneChangedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Phone> phoneId,
    string number,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid PhoneId { get; } = phoneId.Value;
    public string Number { get; } = number;
}
