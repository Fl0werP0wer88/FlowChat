namespace FlowChat.Core.Messaging.SocialGraphService.Events;

public sealed record ContactDeletedIntegrationEvent : IntegrationEvent
{
    public Guid OwnerUserId { get; init; }
    public Guid ContactUserId { get; init; }
}
