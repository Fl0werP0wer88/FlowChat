namespace FlowChat.Core.Messaging.SocialGraphService.Events;

public sealed class ContactAddedIntegrationEvent : IntegrationEvent
{
    public Guid OwnerUserId { get; init; }
    public Guid ContactUserId { get; init; }
}
