namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed record UserProfileDeletedIntegrationEvent : IntegrationEvent
{
    public Guid UserProfileId { get; init; }
}
