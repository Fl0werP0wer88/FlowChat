using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events.Contracts;

namespace FlowChat.UserProfileService.Domain.Events;

public sealed class MainEmailChangedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Email> emailId,
    string address,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid EmailId { get; } = emailId.Value;
    public string Address { get; } = address;
}
