using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events.Contracts;

namespace FlowChat.UserProfileService.Domain.Events;

public sealed class MainEmailChangedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Email> emailId,
    EmailAddress address,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Id<UserProfile> UserProfileId { get; } = aggregateId;
    public Id<Email> EmailId { get; } = emailId;
    public EmailAddress Address { get; } = address;
}
