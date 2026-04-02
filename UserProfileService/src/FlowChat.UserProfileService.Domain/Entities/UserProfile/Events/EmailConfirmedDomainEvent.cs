using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

public sealed class EmailConfirmedDomainEvent(
    Id<UserProfile> aggregateId,
    Id<Email> emailId,
    EmailAddress email,
    bool isAuth,
    DateTimeOffset? occurredOnUtc = null) : BaseUserProfileDomainEvent(aggregateId, occurredOnUtc)
{
    public Id<UserProfile> UserProfileId { get; } = aggregateId;
    public Id<Email> EmailId { get; } = emailId;
    public EmailAddress Email { get; } = email;
    public bool IsAuth { get; } = isAuth;
}
