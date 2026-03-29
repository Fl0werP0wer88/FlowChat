using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events.Contracts;

namespace FlowChat.UserProfileService.Domain.Events;

public sealed class EmailAddedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Email> emailId,
    EmailAddress email,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid UserProfileId { get; } = aggregateId.Value;
    public Guid EmailId { get; } = emailId.Value;
    public EmailAddress Email { get; } = email;
}
