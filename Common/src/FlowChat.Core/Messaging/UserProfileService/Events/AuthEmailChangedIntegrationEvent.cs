namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed record AuthEmailChangedIntegrationEvent : IntegrationEvent
{
    public Guid UserProfileId { get; init; }
    public Guid EmailId { get; init; }
    public required string EmailAddress { get; init; }
}
