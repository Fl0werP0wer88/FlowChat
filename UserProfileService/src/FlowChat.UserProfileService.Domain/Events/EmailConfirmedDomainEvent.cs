using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events.Contracts;

namespace FlowChat.UserProfileService.Domain.Events;

public sealed class EmailConfirmedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Email> emailId,
    string email,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Guid UserProfileId { get; } = aggregateId.Value;
    public Guid EmailId { get; } = emailId.Value;
    public string Email { get; } = email;
}
