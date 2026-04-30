namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed record UserEmailConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserProfileId { get; init; }
    public Guid EmailId { get; init; }
    public required Email Email { get; init; }
}
