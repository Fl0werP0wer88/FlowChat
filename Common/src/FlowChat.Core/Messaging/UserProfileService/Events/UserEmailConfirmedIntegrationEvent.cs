namespace FlowChat.Core.Messaging.UserProfileService.Events;

public sealed class UserEmailConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserProfileId { get; init; }
    public Guid EmailId { get; init; }
    public required string Email { get; init; }
}
